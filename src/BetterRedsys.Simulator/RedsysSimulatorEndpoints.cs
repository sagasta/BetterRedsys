namespace BetterRedsys.Simulator;

/// <summary>
/// Default endpoint path constants used by the BetterRedsys simulator.
/// </summary>
public static class RedsysSimulatorEndpoints
{
    /// <summary>
    /// Default local path for the interactive Redsys simulator payment UI.
    /// </summary>
    public const string DefaultPath = "/redsys-simulator/pay";

    /// <summary>
    /// Default local path for the Redsys server-to-server REST operations simulator (trataPeticionREST).
    /// </summary>
    public const string DefaultRestPath = "/redsys-simulator/rest/trataPeticionREST";
}
