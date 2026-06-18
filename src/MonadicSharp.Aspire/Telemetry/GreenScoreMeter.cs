#nullable enable
using System.Diagnostics.Metrics;

namespace MonadicSharp.Aspire.Telemetry;

/// <summary>
/// OpenTelemetry meter for MonadicSharp pipeline health metrics, surfaced as
/// Green Score dimensions in the Aspire dashboard.
/// </summary>
public sealed class GreenScoreMeter : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _pipelineSuccess;
    private readonly Counter<long> _pipelineFailure;
    private readonly Counter<long> _retryCount;
    private readonly Histogram<double> _pipelineDuration;

    /// <summary>The meter name used to identify this instrument in OTEL collectors.</summary>
    public const string MeterName = "MonadicSharp.GreenScore";

    /// <summary>
    /// Creates a new <see cref="GreenScoreMeter"/> bound to an <see cref="IMeterFactory"/>
    /// (preferred, lifetime-managed by DI).
    /// </summary>
    public GreenScoreMeter(IMeterFactory factory)
    {
        _meter = factory.Create(MeterName);
        RegisterInstruments();
    }

    /// <summary>
    /// Creates a new <see cref="GreenScoreMeter"/> with a standalone meter (for testing).
    /// </summary>
    public GreenScoreMeter()
    {
        _meter = new Meter(MeterName);
        RegisterInstruments();
    }

    [System.Diagnostics.CodeAnalysis.MemberNotNull(
        nameof(_pipelineSuccess), nameof(_pipelineFailure),
        nameof(_retryCount), nameof(_pipelineDuration))]
    private void RegisterInstruments()
    {
        _pipelineSuccess  = _meter.CreateCounter<long>("monadicsharp.pipeline.success",
            description: "Number of successfully completed MonadicSharp pipelines.");
        _pipelineFailure  = _meter.CreateCounter<long>("monadicsharp.pipeline.failure",
            description: "Number of failed MonadicSharp pipelines.");
        _retryCount       = _meter.CreateCounter<long>("monadicsharp.pipeline.retries",
            description: "Total retry attempts across all pipelines.");
        _pipelineDuration = _meter.CreateHistogram<double>("monadicsharp.pipeline.duration",
            unit: "ms",
            description: "End-to-end duration of a MonadicSharp pipeline execution.");
    }

    /// <summary>
    /// Records a successful pipeline completion.
    /// </summary>
    /// <param name="pipelineName">Logical name for the pipeline (e.g. "LlmSummarise").</param>
    /// <param name="durationMs">Wall-clock duration in milliseconds.</param>
    public void RecordSuccess(string pipelineName, double durationMs)
    {
        var tag = new KeyValuePair<string, object?>("pipeline", pipelineName);
        _pipelineSuccess.Add(1, tag);
        _pipelineDuration.Record(durationMs, tag);
    }

    /// <summary>
    /// Records a pipeline failure.
    /// </summary>
    /// <param name="pipelineName">Logical name for the pipeline.</param>
    /// <param name="errorType">The <see cref="ErrorType"/> of the first failure.</param>
    /// <param name="durationMs">Wall-clock duration in milliseconds.</param>
    public void RecordFailure(string pipelineName, ErrorType errorType, double durationMs)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("pipeline",   pipelineName),
            new KeyValuePair<string, object?>("error_type", errorType.ToString())
        };
        _pipelineFailure.Add(1, tags);
        _pipelineDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records a retry attempt within a pipeline.
    /// </summary>
    /// <param name="pipelineName">Logical name for the pipeline.</param>
    /// <param name="attempt">The attempt number (1-based).</param>
    public void RecordRetry(string pipelineName, int attempt) =>
        _retryCount.Add(1,
            new KeyValuePair<string, object?>("pipeline", pipelineName),
            new KeyValuePair<string, object?>("attempt",  attempt));

    /// <summary>
    /// Computes a simple Green Score (0–100) from recent pipeline counters.
    /// Higher = healthier pipeline with fewer retries and failures.
    /// </summary>
    /// <remarks>This is a snapshot heuristic — use Aspire dashboards for full history.</remarks>
    public static double ComputeGreenScore(long successes, long failures, long retries)
    {
        long total = successes + failures;
        if (total == 0) return 100d;

        var successRate = successes / (double)total;
        var retryPenalty = Math.Min(retries / (double)Math.Max(total, 1), 1.0) * 0.2;
        return Math.Round((successRate - retryPenalty) * 100.0, 1);
    }

    /// <inheritdoc/>
    public void Dispose() => _meter.Dispose();
}
