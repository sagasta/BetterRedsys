using BetterRedsys.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Redsys health checks in ASP.NET Core applications.
/// </summary>
public static class RedsysHealthCheckBuilderExtensions
{
    /// <summary>
    /// Adds a health check for the Redsys payment gateway to the health checks builder.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="name">The health check name (defaults to "redsys").</param>
    /// <param name="failureStatus">The <see cref="HealthStatus"/> to report when the gateway is unreachable (defaults to <see cref="HealthStatus.Unhealthy"/>).</param>
    /// <param name="tags">A list of tags for the health check (defaults to ["redsys", "payments"]).</param>
    /// <param name="timeout">Optional timeout for the probe.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/> instance for chaining.</returns>
    public static IHealthChecksBuilder AddRedsys(
        this IHealthChecksBuilder builder,
        string name = "redsys",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        return builder.AddCheck<RedsysHealthCheck>(
            name,
            failureStatus,
            tags ?? ["redsys", "payments"],
            timeout);
    }
}
