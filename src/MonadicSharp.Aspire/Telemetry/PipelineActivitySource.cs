#nullable enable
using System.Diagnostics;

namespace MonadicSharp.Aspire.Telemetry;

/// <summary>
/// OpenTelemetry <see cref="ActivitySource"/> for MonadicSharp pipeline distributed tracing.
/// Provides structured trace spans for LLM pipeline stages — invoke, retry, fallback —
/// correlated with the <see cref="GreenScoreMeter"/> metrics in the Aspire dashboard.
/// </summary>
public static class PipelineActivitySource
{
    /// <summary>The activity source name used to identify spans in OTEL collectors.</summary>
    public const string SourceName = "MonadicSharp.Pipeline";

    private static readonly ActivitySource _source = new(SourceName, "1.0.0");

    /// <summary>
    /// Starts a traced pipeline span. Dispose the returned <see cref="Activity"/> to end the span.
    /// </summary>
    /// <param name="pipelineName">Logical name for the pipeline stage (e.g. "LlmSummarise.Invoke").</param>
    public static Activity? StartPipeline(string pipelineName) =>
        _source.StartActivity(pipelineName, ActivityKind.Internal);

    /// <summary>
    /// Executes <paramref name="work"/> inside a traced span and records the outcome
    /// as <c>ok</c> or <c>error</c> on the span. Propagates the <see cref="Result{T}"/> as-is.
    /// </summary>
    public static async Task<Result<T>> TraceAsync<T>(
        string pipelineName,
        Func<Task<Result<T>>> work,
        CancellationToken ct = default)
    {
        using var activity = _source.StartActivity(pipelineName, ActivityKind.Internal);
        Result<T> result;
        try
        {
            result = await work().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            return Result<T>.Failure(Error.FromException(ex));
        }

        if (result.IsSuccess)
        {
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        else
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.Error.Message);
            activity?.SetTag("error.type", result.Error.Type.ToString());
            activity?.SetTag("error.code", result.Error.Code);
        }

        return result;
    }

#if NET11_0_OR_GREATER
    /// <summary>
    /// ValueTask variant of <see cref="TraceAsync{T}"/> for .NET 11+ Runtime Async hot paths.
    /// </summary>
    public static async ValueTask<Result<T>> TraceAsync<T>(
        string pipelineName,
        Func<ValueTask<Result<T>>> work)
    {
        using var activity = _source.StartActivity(pipelineName, ActivityKind.Internal);
        Result<T> result;
        try
        {
            result = await work().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            return Result<T>.Failure(Error.FromException(ex));
        }

        if (result.IsSuccess)
            activity?.SetStatus(ActivityStatusCode.Ok);
        else
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.Error.Message);
            activity?.SetTag("error.type", result.Error.Type.ToString());
            activity?.SetTag("error.code", result.Error.Code);
        }

        return result;
    }
#endif
}
