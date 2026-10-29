using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace BetterRedsys.Diagnostics;

/// <summary>
/// Telemetry, distributed tracing, and metrics for BetterRedsys.
/// Built on .NET runtime primitives (ActivitySource and Meter) with zero third-party dependencies, 100% Native AOT ready.
/// Compatible with OpenTelemetry, Prometheus, Azure Monitor, and Application Insights.
/// </summary>
public static class RedsysTelemetry
{
    /// <summary>
    /// The name of the ActivitySource and Meter ("BetterRedsys").
    /// </summary>
    public const string Name = "BetterRedsys";

    /// <summary>
    /// Telemetry version, taken from the package version so it never drifts from the released assembly.
    /// </summary>
    public static readonly string Version = GetPackageVersion();

    /// <summary>
    /// ActivitySource for distributed tracing and span instrumentation.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(Name, Version);

    /// <summary>
    /// Meter for high-performance metrics counters and histograms.
    /// </summary>
    public static readonly Meter Meter = new(Name, Version);

    /// <summary>
    /// Counter for payment requests generated via <see cref="BetterRedsys.Services.RedsysPaymentBuilder"/>.
    /// </summary>
    public static readonly Counter<long> PaymentsCreatedCounter = Meter.CreateCounter<long>(
        name: "redsys.payments.created",
        unit: "{payments}",
        description: "Number of Redsys payment requests generated and cryptographically signed");

    /// <summary>
    /// Counter for webhook notifications received and processed.
    /// </summary>
    public static readonly Counter<long> WebhooksReceivedCounter = Meter.CreateCounter<long>(
        name: "redsys.webhooks.received",
        unit: "{notifications}",
        description: "Number of Redsys webhook notifications received and cryptographically verified");

    /// <summary>
    /// Counter for direct server-to-server REST operations (refunds, preauthorizations, COF charges, InSite, queries).
    /// </summary>
    public static readonly Counter<long> S2sOperationsCounter = Meter.CreateCounter<long>(
        name: "redsys.s2s.operations",
        unit: "{operations}",
        description: "Number of direct server-to-server REST operations executed against Redsys");

    /// <summary>
    /// Histogram tracking the execution duration of server-to-server REST calls to Redsys in milliseconds.
    /// </summary>
    public static readonly Histogram<double> S2sDurationHistogram = Meter.CreateHistogram<double>(
        name: "redsys.s2s.duration",
        unit: "ms",
        description: "Duration of server-to-server REST operations against Redsys gateway");

    private static string GetPackageVersion()
    {
        var informational = typeof(RedsysTelemetry).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(informational))
        {
            return typeof(RedsysTelemetry).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        }

        // Strip SourceLink build metadata ("2.0.0+abc123")
        var plusIndex = informational.IndexOf('+');
        return plusIndex >= 0 ? informational[..plusIndex] : informational;
    }
}
