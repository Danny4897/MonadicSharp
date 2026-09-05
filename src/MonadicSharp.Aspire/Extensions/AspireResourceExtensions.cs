#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MonadicSharp.Aspire.HealthChecks;
using MonadicSharp.Aspire.Telemetry;

namespace MonadicSharp.Aspire.Extensions;

/// <summary>
/// DI registration extensions for integrating MonadicSharp with Aspire 13.
/// </summary>
public static class AspireResourceExtensions
{
    /// <summary>
    /// Registers MonadicSharp circuit-breaker health checks and the Green Score meter
    /// with the Aspire health-check infrastructure.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="pipelineName">
    /// Logical name for the pipeline. Used as the health-check name in the Aspire dashboard.
    /// </param>
    /// <param name="configure">Optional callback to override <see cref="CircuitBreakerOptions"/>.</param>
    /// <returns>The registered <see cref="MonadicCircuitBreakerHealthCheck"/> instance
    /// for use in pipeline side effects.</returns>
    /// <example>
    /// <code>
    /// var check = builder.Services.AddMonadicSharpHealthChecks("LlmPipeline");
    ///
    /// app.MapPost("/summarise", async (Request req, MyService svc) =>
    ///     (await svc.SummariseAsync(req.Text))
    ///         .Do(_ => check.Record(true))
    ///         .DoError(_ => check.Record(false))
    ///         .ToOkResult());
    /// </code>
    /// </example>
    public static MonadicCircuitBreakerHealthCheck AddMonadicSharpHealthChecks(
        this IServiceCollection services,
        string pipelineName = "MonadicSharp",
        Action<CircuitBreakerOptions>? configure = null)
    {
        var options = new CircuitBreakerOptions();
        configure?.Invoke(options);

        var check = new MonadicCircuitBreakerHealthCheck(options);

        services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    pipelineName,
                    _ => check,
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["monadic", "pipeline", pipelineName.ToLowerInvariant()]));

        services.AddSingleton(check);
        services.AddSingleton<GreenScoreMeter>();

        return check;
    }

    /// <summary>
    /// Registers the <see cref="GreenScoreMeter"/> singleton only (without health checks).
    /// </summary>
    public static IServiceCollection AddMonadicSharpTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<GreenScoreMeter>();
        return services;
    }
}
