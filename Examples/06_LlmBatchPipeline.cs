// Examples — documentation only, not compiled.
// ReSharper disable All
#pragma warning disable CS0168, CS8019, CS1998
using MonadicSharp;
using MonadicSharp.Extensions;
using MonadicSharp.Query.Extensions;

namespace MonadicSharp.Examples;

// ============================================================
// 06 — LLM BATCH PROCESSING  ·  MonadicSharp.Query
//      LINQ-style operators on IEnumerable<Result<T>>
//      for high-throughput inference pipelines
// ============================================================
//
//  MonadicSharp.Query operators:
//    SuccessValues<T>()                    — only successful values
//    FailureErrors<T>()                    — only errors
//    PartitionMap<T,TOut>(projection)      — project successes, bucket failures
//    GroupFailuresByType<T>()              — ILookup<ErrorType, Error>
//    SuccessRate<T>()                      — 0.0–1.0 ratio
//    RequireSuccessRate<T>(threshold)      — Result<IReadOnlyList<T>>
//    ReconcileBy / MergeBy                 — multi-source reconciliation
//    BestOf<T>(scorer)                     — pick best from competing results
//    AggregateAll<T>(sources)              — combine multiple result sequences
// ============================================================

static class LlmBatchExamples
{
    record DocumentChunk(string Id, string Text, int TokenCount);
    record SummaryChunk(string Id, string Summary, double ConfidenceScore);
    record EmbeddingChunk(string Id, float[] Embedding);

    // Simulated LLM call
    static async Task<Result<SummaryChunk>> SummariseAsync(DocumentChunk chunk)
    {
        await Task.Delay(1); // simulate latency
        if (chunk.TokenCount > 4096) return Error.Validation("Chunk exceeds context window", "token_limit");
        if (chunk.Id.StartsWith("err"))  return Error.Create("LLM inference failed", "LLM_ERROR");
        return new SummaryChunk(chunk.Id, $"Summary of {chunk.Id}", 0.85 + new Random().NextDouble() * 0.15);
    }

    static async Task<Result<EmbeddingChunk>> EmbedAsync(DocumentChunk chunk)
    {
        await Task.Delay(1);
        return new EmbeddingChunk(chunk.Id, new float[1536]);
    }

    // ── 6a. Basic batch processing ────────────────────────────────────────

    static async Task BasicBatch(IEnumerable<DocumentChunk> chunks)
    {
        // Process all chunks concurrently
        var results = await Task.WhenAll(chunks.Select(SummariseAsync));

        // Split into successes and failures
        var (successes, failures) = results.Partition();
        Console.WriteLine($"Processed {successes.Count()} / {results.Length} chunks.");

        // Log failures by type
        foreach (var err in failures)
            Console.WriteLine($"  [{err.Type}] {err.Code}: {err.Message}");
    }

    // ── 6b. Quality gate — require ≥80% success rate ─────────────────────

    static async Task<Result<IReadOnlyList<SummaryChunk>>> ProcessWithQualityGate(
        IEnumerable<DocumentChunk> chunks,
        double minimumSuccessRate = 0.80)
    {
        var results = await Task.WhenAll(chunks.Select(SummariseAsync));

        return results.RequireSuccessRate(minimumSuccessRate);
        // Returns Failure if success rate < 80%:
        //   Error.Create("Success rate 65% below required 80%.", "SUCCESS_RATE_BELOW_MINIMUM")
        //        .WithMetadata("successRate", 0.65)
        //        .WithMetadata("requiredRate", 0.80)
    }

    // ── 6c. Project successes while retaining error visibility ────────────

    static async Task ProjectExample(IEnumerable<DocumentChunk> chunks)
    {
        var results = await Task.WhenAll(chunks.Select(SummariseAsync));

        var (dtos, failures) = results.PartitionMap(s => new
        {
            s.Id,
            s.Summary,
            s.ConfidenceScore
        });

        Console.WriteLine($"  Summaries: {dtos.Count}, Failures: {failures.Count}");
    }

    // ── 6d. Group failures for targeted retry ────────────────────────────

    static async Task GroupedRetry(IEnumerable<DocumentChunk> chunks)
    {
        var results = await Task.WhenAll(chunks.Select(SummariseAsync));

        var grouped = results.GroupFailuresByType();

        // Retry transient failures differently from validation errors
        var transient  = grouped[ErrorType.Failure];
        var validation = grouped[ErrorType.Validation];

        Console.WriteLine($"Transient failures to retry: {transient.Count()}");
        Console.WriteLine($"Validation errors (skip):    {validation.Count()}");
    }

    // ── 6e. Multi-source reconciliation ───────────────────────────────────
    //
    //  Merge summaries from two models; prefer Model-A on conflict.

    static async Task ReconcileModels(IEnumerable<DocumentChunk> chunks)
    {
        var modelAResults = await Task.WhenAll(chunks.Select(SummariseAsync));
        var modelBResults = await Task.WhenAll(chunks.Select(SummariseAsync)); // second model

        // Full outer reconcile: ModelA wins on conflict, right-only from B accepted as-is
        var reconciled = modelAResults.ReconcileBy(
            right:     modelBResults,
            leftKey:   l => l.Id,
            rightKey:  r => r.Id,
            leftOnly:  l => l,
            rightOnly: r => r,
            merge:     (a, b) => a.ConfidenceScore >= b.ConfidenceScore ? a : b  // pick higher confidence
        );

        var (merged, errors) = reconciled.Partition();
        Console.WriteLine($"Merged: {merged.Count()}, Errors: {errors.Count()}");
    }

    // ── 6f. Pick the best result from competing model calls ───────────────

    static async Task<Result<SummaryChunk>> BestModelResult(DocumentChunk chunk)
    {
        // Fan-out to 3 models simultaneously
        var results = await Task.WhenAll(
            SummariseAsync(chunk),
            SummariseAsync(chunk),
            SummariseAsync(chunk)
        );

        return results.BestOf(s => s.ConfidenceScore);
    }

    // ── 6g. Full pipeline with quality gate + reconcile ───────────────────

    static async Task<Result<IReadOnlyList<SummaryChunk>>> FullBatchPipeline(
        IEnumerable<DocumentChunk> chunks)
    {
        var chunkList = chunks.ToList();

        // Primary model
        var primary = await Task.WhenAll(chunkList.Select(SummariseAsync));
        var primaryRate = primary.SuccessRate();

        // Fallback if primary quality drops below 70%
        IEnumerable<Result<SummaryChunk>> finalResults = primaryRate >= 0.70
            ? primary
            : (await Task.WhenAll(chunkList.Select(SummariseAsync))); // secondary model

        return finalResults.RequireSuccessRate(0.75);
    }
}
