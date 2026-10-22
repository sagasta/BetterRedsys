namespace BetterRedsys.Configuration;

/// <summary>
/// Provides standard and official endpoint URLs for the Redsys virtual POS payment gateway.
/// </summary>
public static class RedsysEndpoints
{
    /// <summary>
    /// Official production gateway URL for Redsys SIS virtual POS form redirection.
    /// </summary>
    public const string Production = "https://sis.redsys.es/sis/realizarPago";

    /// <summary>
    /// Official test (Sandbox) gateway URL for Redsys form redirection.
    /// </summary>
    public const string Test = "https://sis-t.redsys.es:25443/sis/realizarPago";

    /// <summary>
    /// Official production server-to-server REST endpoint for direct operations (refunds, pre-authorizations, token charges).
    /// </summary>
    public const string RestProduction = "https://sis.redsys.es/sis/rest/trataPeticionREST";

    /// <summary>
    /// Official test (Sandbox) server-to-server REST endpoint for direct operations.
    /// </summary>
    public const string RestTest = "https://sis-t.redsys.es:25443/sis/rest/trataPeticionREST";

    /// <summary>
    /// Default local path for the embedded BetterRedsys DevMock form simulator middleware.
    /// </summary>
    public const string DefaultDevMockPath = "/redsys-mock/pay";

    /// <summary>
    /// Default local path for the embedded BetterRedsys DevMock REST operations simulator.
    /// </summary>
    public const string DefaultDevMockRestPath = "/redsys-mock/rest/trataPeticionREST";
}
