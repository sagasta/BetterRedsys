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
    /// Custom payment gateway URL override (used by testing tools, proxies, or BetterRedsys.Simulator).
    /// If null or whitespace, defaults to Production or Test URL according to <see cref="IsProduction"/>.
    /// </summary>
    public string? CustomGatewayUrl { get; set; }

    /// <summary>
    /// Custom server-to-server REST gateway URL override (used by testing tools, proxies, or BetterRedsys.Simulator).
    /// If null or whitespace, defaults to Production or Test REST URL according to <see cref="IsProduction"/>.
    /// </summary>
    public string? CustomRestGatewayUrl { get; set; }

    /// <summary>
    /// Default transaction type ("0" = Standard Authorization).
    /// </summary>
    public string DefaultTransactionType { get; set; } = "0";

    /// <summary>
    /// Resolves the payment gateway action URL according to the current environment and configuration.
    /// </summary>
    /// <returns>The resolved gateway URL.</returns>
    public string GetPaymentGatewayUrl()
    {
        if (!string.IsNullOrWhiteSpace(CustomGatewayUrl))
        {
            return CustomGatewayUrl;
        }

        return IsProduction ? RedsysEndpoints.Production : RedsysEndpoints.Test;
    }

    /// <summary>
    /// Resolves the server-to-server REST gateway URL (trataPeticionREST) according to the current environment and configuration.
    /// </summary>
    /// <returns>The resolved REST gateway URL.</returns>
    public string GetRestGatewayUrl()
    {
        if (!string.IsNullOrWhiteSpace(CustomRestGatewayUrl))
        {
            return CustomRestGatewayUrl;
        }

        return IsProduction ? RedsysEndpoints.RestProduction : RedsysEndpoints.RestTest;
    }

    /// <summary>
    /// Configures the merchant credentials for the official Redsys Production environment (sis.redsys.es).
    /// </summary>
    /// <param name="fuc">Merchant FUC code assigned by the acquiring bank.</param>
    /// <param name="terminal">Terminal number assigned by the acquiring bank.</param>
    /// <param name="secretKey">Secret Base64 signature key for production.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public RedsysOptions UseProduction(string fuc, string terminal, string secretKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fuc);
        ArgumentException.ThrowIfNullOrWhiteSpace(terminal);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);

        Fuc = fuc;
        Terminal = terminal;
        SecretKey = secretKey;
        IsProduction = true;
        return this;
    }

    /// <summary>
    /// Configures the merchant credentials for the official Redsys Test/Sandbox environment (sis-t.redsys.es:25443).
    /// </summary>
    /// <param name="fuc">Merchant FUC code assigned for testing.</param>
    /// <param name="terminal">Terminal number assigned for testing.</param>
    /// <param name="secretKey">Secret Base64 signature key for testing.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public RedsysOptions UseTest(string fuc, string terminal, string secretKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fuc);
        ArgumentException.ThrowIfNullOrWhiteSpace(terminal);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);

        Fuc = fuc;
        Terminal = terminal;
        SecretKey = secretKey;
        IsProduction = false;
        return this;
    }

    /// <summary>
    /// Configures Euro ("978") as the active currency.
    /// </summary>
    /// <returns>This options instance for fluent chaining.</returns>
    public RedsysOptions WithEuros()
    {
        Currency = "978";
        return this;
    }

    /// <summary>
    /// Configures a specific ISO-4217 numeric currency code (e.g. "978" for EUR, "840" for USD, "826" for GBP).
    /// </summary>
    /// <param name="currencyCode">ISO-4217 currency code.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public RedsysOptions WithCurrency(string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        Currency = currencyCode;
        return this;
    }

    /// <summary>
    /// Configures the default consumer language.
    /// </summary>
    /// <param name="language">The default language.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public RedsysOptions WithDefaultLanguage(RedsysLanguage language)
    {
        DefaultLanguage = language;
        return this;
    }

    /// <summary>
    /// Configures webhook and customer redirection URLs.
    /// </summary>
    /// <param name="notificationUrl">Online server-to-server webhook notification URL (MerchantUrl).</param>
    /// <param name="okUrl">Optional customer return URL upon successful payment (UrlOk).</param>
    /// <param name="koUrl">Optional customer return URL upon canceled or rejected payment (UrlKo).</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public RedsysOptions WithWebhooks(string notificationUrl, string? okUrl = null, string? koUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(notificationUrl);
        MerchantUrl = notificationUrl;
        UrlOk = okUrl;
        UrlKo = koUrl;
        return this;
    }

    /// <summary>
    /// Configures custom gateway endpoint URLs (for reverse proxies or test environments).
    /// </summary>
    /// <param name="paymentUrl">Custom payment gateway URL.</param>
    /// <param name="restUrl">Optional custom REST gateway URL.</param>
    /// <returns>This options instance for fluent chaining.</returns>
    public RedsysOptions WithCustomGateway(string paymentUrl, string? restUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentUrl);
        CustomGatewayUrl = paymentUrl;
        CustomRestGatewayUrl = restUrl;
        return this;
    }

    /// <summary>
    /// Validates that all required configuration values are provided.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if any mandatory parameter is missing or invalid.</exception>
    public void Validate()
    {
        var failures = RedsysOptionsValidator.GetFailures(this);
        if (failures.Count > 0)
        {
            throw new InvalidOperationException($"BetterRedsys: {string.Join(" ", failures)}");
        }
    }
}
