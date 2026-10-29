using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.Diagnostics;
using BetterRedsys.Models;

namespace BetterRedsys.Services;

/// <summary>
/// Fluent builder for constructing and cryptographically signing Redsys and Bizum payment requests.
/// </summary>
public sealed class RedsysPaymentBuilder
{
    private readonly RedsysOptions _options;
    private readonly IRedsysCryptoService _cryptoService;

    private readonly string _order;
    private readonly RedsysAmount _amount;

    private string? _merchantUrl;
    private string? _urlOk;
    private string? _urlKo;
    private string? _payMethods;
    private string? _customerMobile;
    private string? _productDescription;
    private string? _titular;
    private string? _merchantData;
    private string? _consumerLanguage;
    private string? _identifier;
    private string? _terminal;
    private string? _currency;
    private string? _transactionType;
    private string? _cofIni;
    private string? _cofType;
    private string? _idOper;
    private RedsysEmv3Ds? _emv3Ds;

    internal RedsysPaymentBuilder(
        string order,
        RedsysAmount amount,
        RedsysOptions options,
        IRedsysCryptoService cryptoService)
    {
        ValidateOrder(order);

        _order = order;
        _amount = amount;
        _options = options;
        _cryptoService = cryptoService;

        // Inherit default values from options
        _merchantUrl = options.MerchantUrl;
        _urlOk = options.UrlOk;
        _urlKo = options.UrlKo;
        _terminal = options.Terminal;
        _currency = options.Currency;
        _transactionType = options.DefaultTransactionType;
        _consumerLanguage = options.DefaultLanguage.ToRedsysCode();
    }

    /// <summary>
    /// Forces payment exclusively via Bizum (DS_MERCHANT_PAYMETHODS = "z").
    /// </summary>
    public RedsysPaymentBuilder WithBizum()
    {
        _payMethods = "z";
        return this;
    }

    /// <summary>
    /// Forces payment exclusively via Credit/Debit Card (DS_MERCHANT_PAYMETHODS = "C").
    /// </summary>
    public RedsysPaymentBuilder WithCard()
    {
        _payMethods = "C";
        return this;
    }

    /// <summary>
    /// Lets the customer choose any payment method enabled on the terminal (DS_MERCHANT_PAYMETHODS is omitted).
    /// </summary>
    public RedsysPaymentBuilder WithAllPaymentMethods()
    {
        _payMethods = null;
        return this;
    }

    /// <summary>
    /// Omits DS_MERCHANT_PAYMETHODS so the gateway shows every method enabled on the terminal.
    /// Redsys has no value that offers exactly card and Bizum: any value containing "z" goes straight to Bizum,
    /// and whether Bizum appears in the method selector depends on the terminal configuration.
    /// Equivalent to <see cref="WithAllPaymentMethods"/>.
    /// </summary>
    [Obsolete("Redsys cannot guarantee a card-or-Bizum selector. Use WithAllPaymentMethods(), or let the customer choose in your UI and call WithCard() or WithBizum().")]
    public RedsysPaymentBuilder WithCardOrBizum() => WithAllPaymentMethods();

    /// <summary>
    /// Sets the customer mobile phone (DS_MERCHANT_CUSTOMER_MOBILE), used by Paygold / pay-by-link operations to send the payment SMS.
    /// It does not prefill the Bizum phone in redirection payments: the Bizum page asks the customer for it.
    /// Validates that it contains 9 digits starting with 6 or 7 (supports +34 or 0034 international prefixes).
    /// </summary>
    /// <param name="phone">Customer mobile phone number.</param>
    public RedsysPaymentBuilder WithCustomerPhone(string phone)
    {
        _customerMobile = SanitizeSpanishMobilePhone(phone);
        return this;
    }

    /// <summary>
    /// Sets the merchant product/service description displayed to the customer (max 125 characters).
    /// </summary>
    public RedsysPaymentBuilder WithDescription(string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        _productDescription = Truncate(description, 125);
        return this;
    }

    /// <summary>
    /// Sets the URL where the customer is redirected upon successful payment authorization.
    /// </summary>
    public RedsysPaymentBuilder WithSuccessUrl(string url)
    {
        _urlOk = url;
        return this;
    }

    /// <summary>
    /// Sets the URL where the customer is redirected upon canceled or failed payment.
    /// </summary>
    public RedsysPaymentBuilder WithCancelUrl(string url)
    {
        _urlKo = url;
        return this;
    }

    /// <summary>
    /// Sets the server-to-server online webhook notification URL (HTTP POST from Redsys).
    /// </summary>
    public RedsysPaymentBuilder WithMerchantUrl(string url)
    {
        _merchantUrl = url;
        return this;
    }

    /// <summary>
    /// Sets the cardholder or customer name shown on the gateway (max 60 characters).
    /// </summary>
    public RedsysPaymentBuilder WithTitular(string titular)
    {
        ArgumentNullException.ThrowIfNull(titular);
        _titular = Truncate(titular, 60);
        return this;
    }

    /// <summary>
    /// Sets optional merchant private data (Ds_MerchantData) that Redsys returns intact in the notification webhook.
    /// </summary>
    public RedsysPaymentBuilder WithMerchantData(string data)
    {
        _merchantData = data;
        return this;
    }

    /// <summary>
    /// Sets the consumer language of the Redsys payment gateway using a strongly-typed <see cref="RedsysLanguage"/>.
    /// </summary>
    /// <param name="language">The desired gateway language.</param>
    public RedsysPaymentBuilder WithConsumerLanguage(RedsysLanguage language)
    {
        _consumerLanguage = language.ToRedsysCode();
        return this;
    }

    /// <summary>
    /// Sets the consumer language of the Redsys payment gateway using a <see cref="CultureInfo"/>.
    /// </summary>
    public RedsysPaymentBuilder WithConsumerLanguage(CultureInfo culture)
    {
        _consumerLanguage = RedsysLanguageExtensions.FromCultureInfo(culture).ToRedsysCode();
        return this;
    }

    /// <summary>
    /// Sets the consumer language of the Redsys payment gateway by its official 3-digit code (e.g., "001" for Spanish, "002" for English).
    /// </summary>
    public RedsysPaymentBuilder WithConsumerLanguage(string languageCode)
    {
        _consumerLanguage = languageCode;
        return this;
    }

    /// <summary>
    /// Overrides the transaction type ("0" for standard authorization by default).
    /// </summary>
    public RedsysPaymentBuilder WithTransactionType(string transactionType)
    {
        _transactionType = transactionType;
        return this;
    }

    /// <summary>
    /// Overrides the merchant terminal for this specific transaction.
    /// </summary>
    public RedsysPaymentBuilder WithTerminal(string terminal)
    {
        _terminal = terminal;
        return this;
    }

    /// <summary>
    /// Overrides the ISO-4217 currency code (defaults to 978 for EUR).
    /// </summary>
    public RedsysPaymentBuilder WithCurrency(string currency)
    {
        _currency = currency;
        return this;
    }

    /// <summary>
    /// Sets a reference identifier for recurring or credential-on-file (COF) card tokenization.
    /// </summary>
    public RedsysPaymentBuilder WithIdentifier(string identifier)
    {
        _identifier = identifier;
        return this;
    }

    /// <summary>
    /// Configures the transaction as an initial Card-On-File (COF) transaction where the cardholder authorizes
    /// credentials to be stored for future recurring, installment, or 1-click charges (DS_MERCHANT_COF_INI = "S").
    /// </summary>
    /// <param name="type">The intended future charge type (defaults to Recurring).</param>
    public RedsysPaymentBuilder WithInitialCardOnFile(RedsysCofType type = RedsysCofType.Recurring)
    {
        _cofIni = "S";
        _cofType = type.ToRedsysCode();
        return this;
    }

    /// <summary>
    /// Configures a subsequent merchant-initiated or customer-initiated charge using previously stored card credentials (token).
    /// </summary>
    /// <param name="cardToken">The token returned in Ds_Merchant_Identifier from the initial transaction.</param>
    /// <param name="type">The type of charge (defaults to Recurring).</param>
    public RedsysPaymentBuilder WithStoredCardCharge(string cardToken, RedsysCofType type = RedsysCofType.Recurring)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardToken);

        _identifier = cardToken;
        _cofIni = "N";
        _cofType = type.ToRedsysCode();
        return this;
    }

    /// <summary>
    /// Configures EMV 3-D Secure (3DS2 / PSD2 SCA) customer context or exemption parameters (DS_MERCHANT_EMV3DS).
    /// </summary>
    /// <param name="emv3Ds">The strongly-typed EMV3DS metadata object.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public RedsysPaymentBuilder WithEmv3Ds(RedsysEmv3Ds emv3Ds)
    {
        ArgumentNullException.ThrowIfNull(emv3Ds);
        _emv3Ds = emv3Ds.Clone();
        return this;
    }

    /// <summary>
    /// Configures an EMV 3-D Secure exemption request (e.g. low-value &lt;30 EUR or transaction risk analysis TRA).
    /// </summary>
    /// <param name="challengePreference">The challenge preference indicator (defaults to <see cref="Redsys3DsChallengePreference.ExemptionLowValue"/>).</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public RedsysPaymentBuilder With3DsExemption(string challengePreference = Redsys3DsChallengePreference.ExemptionLowValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(challengePreference);
        _emv3Ds ??= new RedsysEmv3Ds();
        _emv3Ds.ThreeDSRequestorChallengeInd = challengePreference;
        return this;
    }

    /// <summary>
    /// Configures a Redsys InSite operation token (idOper) obtained from the frontend redsys.js iframe.
    /// </summary>
    /// <param name="idOper">The temporary operation token generated by Redsys InSite.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public RedsysPaymentBuilder WithInsite(string idOper)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idOper);
        _idOper = idOper;
        return this;
    }

    /// <summary>
    /// Exports the serialized and cryptographically signed parameters in a <see cref="RedsysPaymentResponseData"/> DTO
    /// ready to be consumed by modern decoupled front-ends (React, Next.js, Blazor, Vue, Mobile Apps).
    /// </summary>
    public RedsysPaymentResponseData ToFormData()
    {
        _options.Validate();

        using var activity = RedsysTelemetry.ActivitySource.StartActivity("Redsys.CreatePayment");
        activity?.SetTag("redsys.order", _order);
        activity?.SetTag("redsys.amount", _amount.ToDecimal());
        activity?.SetTag("redsys.currency", _currency ?? _options.Currency);
        activity?.SetTag("redsys.fuc", _options.Fuc);
        activity?.SetTag("redsys.terminal", _terminal ?? _options.Terminal);
        activity?.SetTag("redsys.transaction_type", _transactionType ?? _options.DefaultTransactionType);

        RedsysTelemetry.PaymentsCreatedCounter.Add(1,
            new KeyValuePair<string, object?>("currency", _currency ?? _options.Currency),
            new KeyValuePair<string, object?>("transaction_type", _transactionType ?? _options.DefaultTransactionType));

        var dto = new RedsysMerchantParametersDto
        {
            Amount = _amount.ToRedsysString(),
            Order = _order,
            MerchantCode = _options.Fuc,
            Currency = _currency ?? _options.Currency,
            TransactionType = _transactionType ?? _options.DefaultTransactionType,
            Terminal = _terminal ?? _options.Terminal,
            MerchantUrl = _merchantUrl,
            UrlOk = _urlOk,
            UrlKo = _urlKo,
            PayMethods = _payMethods,
            CustomerMobile = _customerMobile,
            ProductDescription = _productDescription,
            Titular = _titular,
            MerchantData = _merchantData != null ? Uri.EscapeDataString(_merchantData) : null,
            ConsumerLanguage = _consumerLanguage,
            Identifier = _identifier,
            CofIni = _cofIni,
            CofType = _cofType,
            IdOper = _idOper,
            Emv3Ds = _emv3Ds
        };

        // Serialize using System.Text.Json Source Generator (Zero reflection, Native AOT)
        var jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(dto, RedsysJsonContext.Default.RedsysMerchantParametersDto);
        var merchantParametersBase64 = Convert.ToBase64String(jsonUtf8Bytes);

        // Derive 3DES key with zero IV and compute HMAC-SHA256 signature
        var diversifiedKey = _cryptoService.DiversifyKey(_options.SecretKey, _order);
        var signature = _cryptoService.ComputeSignature(merchantParametersBase64, diversifiedKey);

        var actionUrl = _options.GetPaymentGatewayUrl();

        return new RedsysPaymentResponseData(
            SignatureVersion: "HMAC_SHA256_V1",
            MerchantParameters: merchantParametersBase64,
            Signature: signature,
            ActionUrl: actionUrl,
            Order: _order,
            Amount: _amount);
    }

    /// <summary>
    /// Generates a complete, stylized, and self-submitting HTML page that submits the Redsys form automatically on load.
    /// Supports English and Spanish localization based on the configured consumer language or explicit parameter.
    /// </summary>
    /// <param name="language">Explicit language for texts (defaults to the builder's consumer language, or Spanish).</param>
    /// <param name="submitButtonText">Optional override for the fallback submit button text.</param>
    /// <param name="cspNonce">Optional Content-Security-Policy nonce applied to the inline style and script, for pages served under a strict CSP.</param>
    public string ToAutoSubmitHtml(RedsysLanguage? language = null, string? submitButtonText = null, string? cspNonce = null)
    {
        var data = ToFormData();

        var effectiveLang = language ?? RedsysLanguageExtensions.FromRedsysCode(_consumerLanguage);
        var texts = RedsysBusinessTexts.GetHtmlTexts(effectiveLang);

        var pageTitle = texts.PageTitle;
        var heading = texts.Heading;
        var subheading = texts.Subheading;
        var labelOrder = texts.OrderLabel;
        var labelAmount = texts.AmountLabel;
        var buttonText = submitButtonText ?? texts.DefaultButtonText;
        var htmlLang = effectiveLang.GetIsoCode();

        var actionUrl = WebUtility.HtmlEncode(data.ActionUrl);
        var signatureVersion = WebUtility.HtmlEncode(data.SignatureVersion);
        var merchantParameters = WebUtility.HtmlEncode(data.MerchantParameters);
        var signature = WebUtility.HtmlEncode(data.Signature);
        var escapedButtonText = WebUtility.HtmlEncode(buttonText);
        var displayAmount = WebUtility.HtmlEncode(data.Amount.ToDisplayString());
        var escapedOrder = WebUtility.HtmlEncode(data.Order);
        var nonceAttribute = string.IsNullOrWhiteSpace(cspNonce) ? string.Empty : $" nonce=\"{WebUtility.HtmlEncode(cspNonce)}\"";

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine($"<html lang=\"{htmlLang}\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"utf-8\" />");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        sb.AppendLine($"    <title>{pageTitle}</title>");
        sb.AppendLine($"    <style{nonceAttribute}>");
        sb.AppendLine("        :root { --primary: #0066cc; --bg: #0f172a; --card: #1e293b; --text: #f8fafc; --muted: #94a3b8; }");
        sb.AppendLine("        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: var(--bg); color: var(--text); display: flex; align-items: center; justify-content: center; min-height: 100vh; margin: 0; padding: 20px; box-sizing: border-box; }");
        sb.AppendLine("        .card { background: var(--card); border: 1px solid rgba(255,255,255,0.08); border-radius: 16px; padding: 40px; max-width: 440px; width: 100%; text-align: center; box-shadow: 0 20px 40px rgba(0,0,0,0.4); backdrop-filter: blur(8px); }");
        sb.AppendLine("        .spinner { width: 48px; height: 48px; border: 4px solid rgba(255,255,255,0.1); border-top-color: var(--primary); border-radius: 50%; animation: spin 0.8s linear infinite; margin: 0 auto 24px auto; }");
        sb.AppendLine("        @keyframes spin { to { transform: rotate(360deg); } }");
        sb.AppendLine("        h1 { font-size: 1.25rem; font-weight: 600; margin: 0 0 8px 0; }");
        sb.AppendLine("        p { font-size: 0.95rem; color: var(--muted); margin: 0 0 20px 0; line-height: 1.5; }");
        sb.AppendLine("        .details { background: rgba(0,0,0,0.25); border-radius: 8px; padding: 12px 16px; margin-bottom: 24px; font-size: 0.9rem; text-align: left; }");
        sb.AppendLine("        .details-row { display: flex; justify-content: space-between; margin: 4px 0; }");
        sb.AppendLine("        .details-label { color: var(--muted); }");
        sb.AppendLine("        .details-val { font-weight: 600; }");
        sb.AppendLine("        button { background: var(--primary); color: white; border: none; padding: 12px 24px; border-radius: 8px; font-size: 0.95rem; font-weight: 500; cursor: pointer; width: 100%; transition: background 0.2s ease; }");
        sb.AppendLine("        button:hover { background: #0052a3; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"card\">");
        sb.AppendLine("        <div class=\"spinner\"></div>");
        sb.AppendLine($"        <h1>{heading}</h1>");
        sb.AppendLine($"        <p>{subheading}</p>");
        sb.AppendLine("        <div class=\"details\">");
        sb.AppendLine($"            <div class=\"details-row\"><span class=\"details-label\">{labelOrder}</span><span class=\"details-val\">{escapedOrder}</span></div>");
        sb.AppendLine($"            <div class=\"details-row\"><span class=\"details-label\">{labelAmount}</span><span class=\"details-val\">{displayAmount}</span></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine($"        <form id=\"redsys-form\" method=\"POST\" action=\"{actionUrl}\">");
        sb.AppendLine($"            <input type=\"hidden\" name=\"{RedsysPaymentResponseData.FormFieldSignatureVersion}\" value=\"{signatureVersion}\" />");
        sb.AppendLine($"            <input type=\"hidden\" name=\"{RedsysPaymentResponseData.FormFieldMerchantParameters}\" value=\"{merchantParameters}\" />");
        sb.AppendLine($"            <input type=\"hidden\" name=\"{RedsysPaymentResponseData.FormFieldSignature}\" value=\"{signature}\" />");
        sb.AppendLine($"            <noscript><button type=\"submit\">{escapedButtonText}</button></noscript>");
        sb.AppendLine("        </form>");
        sb.AppendLine("    </div>");
        sb.AppendLine($"    <script{nonceAttribute}>");
        sb.AppendLine("        (function() {");
        sb.AppendLine("            var form = document.getElementById('redsys-form');");
        sb.AppendLine("            if (form) { form.submit(); }");
        sb.AppendLine("        })();");
        sb.AppendLine("    </script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a complete, stylized, and self-submitting HTML page using an explicit fallback button text.
    /// </summary>
    /// <param name="submitButtonText">Visible text in case JavaScript is disabled in the browser.</param>
    public string ToAutoSubmitHtml(string submitButtonText) => ToAutoSubmitHtml(null, submitButtonText);

    /// <summary>
    /// Truncates to a maximum UTF-16 length without splitting a surrogate pair (e.g. an emoji) in half.
    /// </summary>
    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        var length = char.IsHighSurrogate(value[maxLength - 1]) ? maxLength - 1 : maxLength;
        return value[..length];
    }

    /// <summary>
    /// Validates that the order identifier matches strict Redsys rules (4-12 alphanumeric characters, first 4 numeric).
    /// </summary>
    public static void ValidateOrder(string order)
    {
        RedsysOrderNumber.Validate(order);
    }

    /// <summary>
    /// Sanitizes and validates a Spanish mobile phone number for Bizum.
    /// </summary>
    public static string SanitizeSpanishMobilePhone(string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        // Extract only ASCII digits
        var digits = new string(phone.Where(char.IsAsciiDigit).ToArray());

        // Handle common international country code prefixes (+34, 0034, 34)
        if (digits.StartsWith("0034") && digits.Length == 13)
        {
            digits = digits[4..];
        }
        else if (digits.StartsWith("34") && digits.Length == 11)
        {
            digits = digits[2..];
        }

        // A valid Spanish mobile phone must have 9 digits starting with 6 or 7
        if (digits.Length != 9 || (digits[0] != '6' && digits[0] != '7'))
        {
            throw new ArgumentException(
                $"Phone number '{phone}' is not a valid Spanish mobile number. It must contain 9 digits starting with 6 or 7.",
                nameof(phone));
        }

        return digits;
    }
}
