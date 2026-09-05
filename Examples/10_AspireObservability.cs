// Examples — documentation only, not compiled.
// ReSharper disable All
#pragma warning disable CS0168, CS8019, CS1998
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MonadicSharp;
using MonadicSharp.Aspire.Extensions;
using MonadicSharp.Aspire.Telemetry;

namespace MonadicSharp.Examples;

// ============================================================
// 10 — ASPIRE OBSERVABILITY  ·  MonadicSharp.Aspire
//      Circuit breaker health checks + Green Score OTel metrics
//      + distributed tracing with ActivitySource
// ============================================================
//
//  Components:
//    MonadicCircuitBreakerHealthCheck  — sliding-window failure rate
//    GreenScoreMeter                   — OTEL counters + histogram
//    PipelineActivitySource            — distributed traces per stage
//
//  Aspire Dashboard shows:
//    • Health → circuit open/degraded/healthy
//    • Metrics → success/failure/retry/duration per pipeline
//    • Traces → per-request pipeline span tree
// ============================================================

static class AspireObservabilityExamples
{
    record SummariseRequest(string DocumentId, string Text);
    record SummariseResponse(string DocumentId, string Summary);

    // ── 10a. Registration (Program.cs) ────────────────────────────────────

    static void RegisterServices(WebApplicationBuilder builder)
    {
        // One-liner: registers IHealthCheck + GreenScoreMeter + ActivitySource
        var circuitBreaker = builder.Services.AddMonadicSharpHealthChecks(
            pipelineName: "LlmSummarise",
            configure: opts =>
            {
                opts.FailureThreshold  = 0.40;  // open circuit at 40% failure rate
                opts.DegradedThreshold = 0.20;  // degrade at 20%
                opts.WindowSize        = 30;    // last 30 observations
                opts.MinimumSampleSize = 5;     // need ≥5 samples before evaluating
            });

        // Optional: add Aspire standard health check UI
        builder.Services.AddHealthChecks();
    }

    // ── 10b. Pipeline wired with circuit breaker + telemetry ─────────────

    class SummariseService
    {
        private readonly MonadicCircuitBreakerHealthCheck _cb;
        private readonly GreenScoreMeter _meter;
        private readonly ILlmClient _llm;

        public SummariseService(
            MonadicCircuitBreakerHealthCheck cb,
            GreenScoreMeter meter,
            ILlmClient llm)
        {
            _cb    = cb;
            _meter = meter;
            _llm   = llm;
        }

        public async Task<Result<SummariseResponse>> SummariseAsync(SummariseRequest req)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Wrap in a named trace span
            var result = await PipelineActivitySource.TraceAsync(
                "LlmSummarise.Invoke",
                async () => await _llm.SummariseAsync(req.Text));

            sw.Stop();

            // Record to both circuit breaker (health) and meter (metrics)
            _cb.Record(result.IsSuccess);

            if (result.IsSuccess)
                _meter.RecordSuccess("LlmSummarise", sw.Elapsed.TotalMilliseconds);
            else
                _meter.RecordFailure("LlmSummarise", result.Error.Type, sw.Elapsed.TotalMilliseconds);

            return result.Map(summary => new SummariseResponse(req.DocumentId, summary));
        }
    }

    // ── 10c. Minimal API endpoint with circuit breaker side-effects ────────

    static void MapEndpoints(WebApplication app, SummariseService svc)
    {
        app.MapPost("/summarise", async (SummariseRequest req) =>
            (await svc.SummariseAsync(req)).ToOkResult());

        // Health check exposed at /health (standard Aspire endpoint)
        // {
        //   "status": "Degraded",
        //   "results": {
        //     "LlmSummarise": {
        //       "status": "Degraded",
        //       "description": "Circuit degraded: 23.3% failure rate.",
        //       "data": { "failureRate": "23.3%", "samples": 30, "threshold": "40%" }
        //     }
        //   }
        // }
    }

    // ── 10d. Green Score snapshot ─────────────────────────────────────────

    static void ComputeScore(GreenScoreMeter meter)
    {
        double score = GreenScoreMeter.ComputeGreenScore(
            successes: 950,
            failures:  50,
            retries:   80
        );
        Console.WriteLine($"Green Score: {score}/100");  // e.g. 87.6
        // Higher = healthier (fewer failures, fewer retries relative to volume)
    }

    // ── 10e. Multi-pipeline observability ─────────────────────────────────

    class OrchestrationService
    {
        private readonly GreenScoreMeter _meter;
        private readonly ILlmClient _llm;

        public OrchestrationService(GreenScoreMeter meter, ILlmClient llm)
        {
            _meter = meter;
            _llm   = llm;
        }

        public async Task<Result<string>> ProcessDocumentAsync(string docId, string text)
        {
            // Stage 1: summarise
            var summarise = await PipelineActivitySource.TraceAsync(
                "Document.Summarise",
                async () => await _llm.SummariseAsync(text));

            _meter.RecordSuccess("Document.Summarise", 142.5);

            if (summarise.IsFailure)
            {
                _meter.RecordFailure("Document.Summarise", summarise.Error.Type, 5200);
                return Result<string>.Failure(summarise.Error);
            }

            // Stage 2: embed (with retry tracking)
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                var embed = await PipelineActivitySource.TraceAsync(
                    $"Document.Embed.attempt{attempt}",
                    async () => await _llm.EmbedAsync(summarise.Value));

                if (embed.IsSuccess) return embed.Map(e => $"Processed: {e}");

                _meter.RecordRetry("Document.Embed", attempt);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }

            return Result<string>.Failure(Error.Create("Embedding failed after 3 attempts", "EMBED_EXHAUSTED"));
        }
    }

    interface ILlmClient
    {
        Task<Result<string>> SummariseAsync(string text);
        Task<Result<string>> EmbedAsync(string text);
    }
}

// Stub for .ToOkResult() reference (from MonadicSharp.Interop)
static class ResultExtensionStub
{
    public static Microsoft.AspNetCore.Http.IResult ToOkResult<T>(this Result<T> r) => throw new();
}
