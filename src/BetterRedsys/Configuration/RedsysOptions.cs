using BetterRedsys.Models;

namespace BetterRedsys.Configuration;

/// <summary>
/// Configuration options for the Redsys and Bizum payment gateway integration.
/// </summary>
public sealed class RedsysOptions
{
    /// <summary>
    /// Merchant FUC code assigned by the acquiring bank (e.g., "999008881").
    /// </summary>
    public string Fuc { get; set; } = string.Empty;

    /// <summary>
    /// Terminal number assigned by the acquiring bank (typically "1" or "001").
    /// </summary>
    public string Terminal { get; set; } = "1";

    /// <summary>
    /// Secret merchant signature key in Base64 format obtained from the Redsys administration portal.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// ISO-4217 numeric currency code (defaults to 978 for Euro).
    /// </summary>
    public string Currency { get; set; } = "978";

    /// <summary>
    /// Default consumer language configured for the merchant (defaults to <see cref="RedsysLanguage.Spanish"/>).
    /// </summary>
    public RedsysLanguage DefaultLanguage { get; set; } = RedsysLanguage.Spanish;

    /// <summary>
    /// Merchant online server-to-server webhook notification URL (HTTP POST from Redsys).
    /// </summary>
    public string? MerchantUrl { get; set; }

    /// <summary>
    /// Return URL where the customer is redirected upon successful payment authorization.
    /// </summary>
    public string? UrlOk { get; set; }

    /// <summary>
    /// Return URL where the customer is redirected upon canceled or failed payment.
    /// </summary>
    public string? UrlKo { get; set; }

    /// <summary>
    /// Indicates whether to operate in real Production mode (true) or Test/Sandbox mode (false).
    /// </summary>
    public bool IsProduction { get; set; }

    /// <summary>
    /// Enables the embedded DevMock simulator middleware for local end-to-end debugging without ngrok or bank sandbox.
    /// </summary>
    public bool UseDevMock { get; set; }

    /// <summary>
    /// Relative URL path where the embedded DevMock interceptor responds.
    /// </summary>
    public string DevMockPath { get; set; } = RedsysEndpoints.DefaultDevMockPath;

    /// <summary>
    /// Relative URL path where the embedded DevMock REST operations simulator responds.
    /// </summary>
    public string DevMockRestPath { get; set; } = RedsysEndpoints.DefaultDevMockRestPath;

    /// <summary>
    /// Default transaction type ("0" = Standard Authorization).
    /// </summary>
    public string DefaultTransactionType { get; set; } = "0";

    /// <summary>
    /// Resolves the payment gateway action URL according to the current environment and DevMock configuration.
    /// </summary>
    /// <returns>The resolved gateway URL.</returns>
    public string GetPaymentGatewayUrl()
    {
        if (UseDevMock)
        {
            return DevMockPath;
        }

        return IsProduction ? RedsysEndpoints.Production : RedsysEndpoints.Test;
    }

    /// <summary>
    /// Resolves the server-to-server REST gateway URL (trataPeticionREST) according to the current environment and DevMock configuration.
    /// </summary>
    /// <returns>The resolved REST gateway URL.</returns>
    public string GetRestGatewayUrl()
    {
        if (UseDevMock)
        {
            return DevMockRestPath;
        }

        return IsProduction ? RedsysEndpoints.RestProduction : RedsysEndpoints.RestTest;
    }

    /// <summary>
    /// Validates that all required configuration values are provided.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if any mandatory parameter is missing or invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Fuc))
        {
            throw new InvalidOperationException("BetterRedsys: Merchant FUC code (Fuc) is required.");
        }

        if (string.IsNullOrWhiteSpace(Terminal))
        {
            throw new InvalidOperationException("BetterRedsys: Terminal number (Terminal) is required.");
        }

        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new InvalidOperationException("BetterRedsys: Secret Base64 key (SecretKey) is required.");
        }

        if (string.IsNullOrWhiteSpace(Currency))
        {
            throw new InvalidOperationException("BetterRedsys: Currency code (Currency) is required.");
        }
    }
}
