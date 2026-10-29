namespace BetterRedsys.Simulator;

/// <summary>
/// Configuration options for the BetterRedsys in-memory simulator.
/// </summary>
public sealed class RedsysSimulatorOptions
{
    /// <summary>
    /// Relative URL path where the Redsys interactive payment simulator responds.
    /// Defaults to "/redsys-simulator/pay".
    /// </summary>
    public string Path { get; set; } = RedsysSimulatorEndpoints.DefaultPath;

    /// <summary>
    /// Relative URL path where the Redsys REST operations simulator responds.
    /// Defaults to "/redsys-simulator/rest/trataPeticionREST".
    /// </summary>
    public string RestPath { get; set; } = RedsysSimulatorEndpoints.DefaultRestPath;

    /// <summary>
    /// Indicates whether to automatically set <see cref="BetterRedsys.Configuration.RedsysOptions.CustomGatewayUrl"/>
    /// and <see cref="BetterRedsys.Configuration.RedsysOptions.CustomRestGatewayUrl"/> to point to the simulator.
    /// Defaults to true.
    /// </summary>
    public bool AutoConfigureGatewayUrls { get; set; } = true;

    /// <summary>
    /// Allows the simulator to intercept requests when the host environment is not Development (e.g. Staging).
    /// The simulator signs arbitrary notifications with the configured secret key, so anyone able to reach it can
    /// forge webhooks against your application. Only enable this on environments that are not publicly reachable.
    /// Defaults to false. The simulator never runs when <see cref="BetterRedsys.Configuration.RedsysOptions.IsProduction"/> is true.
    /// </summary>
    public bool AllowNonDevelopmentEnvironments { get; set; }
}
