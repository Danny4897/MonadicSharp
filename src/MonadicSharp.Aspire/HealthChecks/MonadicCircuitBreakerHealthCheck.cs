#nullable enable
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MonadicSharp.Aspire.HealthChecks;

/// <summary>
/// Stateful circuit-breaker health check for MonadicSharp pipelines.
/// Integrates with the Aspire 13 dashboard and standard ASP.NET Core health-check
/// endpoint (<c>/health</c>).
/// </summary>
/// <remarks>
/// The circuit opens (Unhealthy) when the failure rate over the last
/// <see cref="WindowSize"/> observations exceeds <see cref="FailureThreshold"/>.
/// Register via <see cref="Extensions.AspireResourceExtensions.AddMonadicSharpHealthChecks"/>.
/// </remarks>
public sealed class MonadicCircuitBreakerHealthCheck : IHealthCheck
{
    private readonly CircuitBreakerOptions _options;
    private readonly Queue<bool> _window = new();
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new health check with the given options.
    /// </summary>
    public MonadicCircuitBreakerHealthCheck(CircuitBreakerOptions options) =>
        _options = options;

    /// <summary>
    /// Records a pipeline outcome (success or failure) and updates the sliding window.
    /// Call this from your MonadicSharp pipeline via a <c>Do</c> / <c>DoError</c> side effect.
    /// </summary>
    public void Record(bool success)
    {
        lock (_lock)
        {
            _window.Enqueue(success);
            while (_window.Count > _options.WindowSize)
                _window.Dequeue();
        }
    }

    /// <summary>
    /// Records the outcome of a <c>Result&lt;T&gt;</c> and returns it unchanged —
    /// designed for fluent pipeline composition via <c>.Do/.DoError</c>.
    /// </summary>
    public Result<T> Record<T>(Result<T> result)
    {
        Record(result.IsSuccess);
        return result;
    }

    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        HealthCheckResult health;

        lock (_lock)
        {
            if (_window.Count < _options.MinimumSampleSize)
            {
                health = HealthCheckResult.Healthy(
                    $"Warming up — {_window.Count}/{_options.MinimumSampleSize} samples collected.");
            }
            else
            {
                var failureRate = _window.Count(r => !r) / (double)_window.Count;
                var data = new Dictionary<string, object>
                {
                    ["failureRate"] = $"{failureRate:P1}",
                    ["samples"]     = _window.Count,
                    ["threshold"]   = $"{_options.FailureThreshold:P0}"
                };

                if (failureRate >= _options.FailureThreshold)
                    health = HealthCheckResult.Unhealthy(
                        $"Circuit open: {failureRate:P1} failure rate exceeds {_options.FailureThreshold:P0} threshold.",
                        data: data);
                else if (failureRate >= _options.DegradedThreshold)
                    health = HealthCheckResult.Degraded(
                        $"Circuit degraded: {failureRate:P1} failure rate.",
                        data: data);
                else
                    health = HealthCheckResult.Healthy(
                        $"Circuit closed: {failureRate:P1} failure rate.",
                        data: data);
            }
        }

        return Task.FromResult(health);
    }
}

/// <summary>
/// Configuration for <see cref="MonadicCircuitBreakerHealthCheck"/>.
/// </summary>
public sealed record CircuitBreakerOptions
{
    /// <summary>Number of recent observations in the sliding window. Default: 20.</summary>
    public int WindowSize { get; init; } = 20;

    /// <summary>Minimum number of samples before the circuit evaluates. Default: 5.</summary>
    public int MinimumSampleSize { get; init; } = 5;

    /// <summary>Failure rate (0–1) that opens the circuit (Unhealthy). Default: 0.5.</summary>
    public double FailureThreshold { get; init; } = 0.5;

    /// <summary>Failure rate (0–1) that degrades the circuit (Degraded). Default: 0.25.</summary>
    public double DegradedThreshold { get; init; } = 0.25;
}
