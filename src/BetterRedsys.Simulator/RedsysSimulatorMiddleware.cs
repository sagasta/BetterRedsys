using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BetterRedsys.Simulator.UI;

namespace BetterRedsys.Simulator;

/// <summary>
/// Development middleware for interactively simulating the Redsys gateway in-memory without ngrok or bank sandbox access.
/// </summary>
public sealed class RedsysSimulatorMiddleware
{
    // Fallback for manual construction without IHttpClientFactory; shared to avoid socket exhaustion from per-request clients
    private static readonly HttpClient SharedHttpClient = new();

    private readonly RequestDelegate _next;
    private readonly RedsysOptions _redsysOptions;
    private readonly RedsysSimulatorOptions _simulatorOptions;
    private readonly IRedsysCryptoService _cryptoService;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IHostEnvironment? _hostEnvironment;
    private readonly ILogger<RedsysSimulatorMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RedsysSimulatorMiddleware"/>.
    /// </summary>
    public RedsysSimulatorMiddleware(
        RequestDelegate next,
        IOptions<RedsysOptions> redsysOptions,
        IOptions<RedsysSimulatorOptions> simulatorOptions,
        IRedsysCryptoService cryptoService,
        ILogger<RedsysSimulatorMiddleware> logger,
        IHttpClientFactory? httpClientFactory = null,
        IHostEnvironment? hostEnvironment = null)
    {
        _next = next;
        _redsysOptions = redsysOptions.Value;
        _simulatorOptions = simulatorOptions.Value;
        _cryptoService = cryptoService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _hostEnvironment = hostEnvironment;
    }

    /// <summary>
    /// The simulator signs forged notifications with the real secret key, so it only runs in Development unless explicitly allowed.
    /// When no <see cref="IHostEnvironment"/> is available (manual construction), only the <see cref="RedsysOptions.IsProduction"/> guard applies.
    /// </summary>
    private bool IsAllowedEnvironment() =>
        _simulatorOptions.AllowNonDevelopmentEnvironments ||
        _hostEnvironment is null ||
        _hostEnvironment.IsDevelopment();

    /// <summary>
    /// Processes incoming HTTP requests, intercepting calls directed to the Redsys simulator.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Production safety guard: do not intercept requests if running in Production
        if (_redsysOptions.IsProduction || !IsAllowedEnvironment())
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        var mockBasePath = _simulatorOptions.Path.TrimEnd('/');

        // Server-to-server REST simulation endpoint (trataPeticionREST)
        if (path.Equals(_simulatorOptions.RestPath, StringComparison.OrdinalIgnoreCase) &&
            HttpMethods.IsPost(context.Request.Method))
        {
            await HandleSimulatorRestOperationAsync(context);
            return;
        }

        if (path.StartsWith(mockBasePath, StringComparison.OrdinalIgnoreCase))
        {
            // Endpoint to dispatch simulated webhook HTTP notification
            if (path.Equals($"{mockBasePath}/dispatch-webhook", StringComparison.OrdinalIgnoreCase) &&
                HttpMethods.IsPost(context.Request.Method))
            {
                await HandleDispatchWebhookAsync(context);
                return;
            }

            // Main simulator endpoint (payment form interception)
            if (HttpMethods.IsPost(context.Request.Method))
            {
                await RenderMockPaymentUiAsync(context);
                return;
            }

            // Direct GET access: status & landing page
            await RenderMockLandingUiAsync(context);
            return;
        }

        await _next(context);
    }

    private async Task HandleSimulatorRestOperationAsync(HttpContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            var restReq = JsonSerializer.Deserialize(body, SimulatorRestJsonContext.Default.SimulatorRestRequestDto);

            if (restReq is null || string.IsNullOrWhiteSpace(restReq.MerchantParameters))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            var normalizedParams = _cryptoService.NormalizeBase64(restReq.MerchantParameters);
            var paramBytes = Convert.FromBase64String(normalizedParams);
            var merchantDto = JsonSerializer.Deserialize(paramBytes, SimulatorRestJsonContext.Default.SimulatorRestMerchantDto);

            if (merchantDto is null || string.IsNullOrWhiteSpace(merchantDto.Order))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            // Determine response code based on transaction type
            // Determine response code based on transaction type
            var responseCode = merchantDto.TransactionType switch
            {
                "3" => "0900", // Refund approved
                "2" => "0000", // Preauthorization confirmed
                "9" => "0400", // Preauthorization canceled
                "J" => "0000", // Consultation / Query approved
                _ => "0000"    // Default direct authorization (standard, InSite, COF)
            };

            string? token = merchantDto.Identifier;
            if (string.IsNullOrWhiteSpace(token) && (merchantDto.CofIni == "S" || !string.IsNullOrWhiteSpace(merchantDto.IdOper)))
            {
                token = "TOK_SIM_123456789";
            }

            var notificationDto = new SimulatorRestNotificationDto
            {
                Date = Uri.EscapeDataString(RedsysTimeZone.Now().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
                Hour = Uri.EscapeDataString(RedsysTimeZone.Now().ToString("HH:mm", CultureInfo.InvariantCulture)),
                Amount = merchantDto.Amount,
                Currency = merchantDto.Currency ?? _redsysOptions.Currency,
                Order = merchantDto.Order,
                MerchantCode = merchantDto.MerchantCode ?? _redsysOptions.Fuc,
                Terminal = merchantDto.Terminal ?? _redsysOptions.Terminal,
                Response = responseCode,
                AuthorisationCode = "888999",
                TransactionType = merchantDto.TransactionType,
                SecurePayment = "1",
                CardCountry = "724",
                CardType = "D",
                CardBrand = "1",
                MerchantIdentifier = token
            };

            var notifBytes = JsonSerializer.SerializeToUtf8Bytes(notificationDto, SimulatorRestJsonContext.Default.SimulatorRestNotificationDto);
            var notifBase64 = Convert.ToBase64String(notifBytes);

            var diversifiedKey = _cryptoService.DiversifyKey(_redsysOptions.SecretKey, merchantDto.Order);
            var signature = _cryptoService.ComputeSignature(notifBase64, diversifiedKey);

            var restResponse = new SimulatorRestResponseDto
            {
                SignatureVersion = "HMAC_SHA256_V1",
                MerchantParameters = notifBase64,
                Signature = signature
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(restResponse, SimulatorRestJsonContext.Default.SimulatorRestResponseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BetterRedsys Simulator: Error processing REST simulation request.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }

    private async Task RenderMockPaymentUiAsync(HttpContext context)
    {
        string? merchantParametersBase64 = null;
        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync();
            merchantParametersBase64 = form[RedsysPaymentResponseData.FormFieldMerchantParameters].ToString();
        }

        SimulatorRestMerchantDto? parameters = null;
        if (!string.IsNullOrWhiteSpace(merchantParametersBase64))
        {
            try
            {
                var normalized = _cryptoService.NormalizeBase64(merchantParametersBase64);
                var jsonBytes = Convert.FromBase64String(normalized);
                parameters = JsonSerializer.Deserialize(jsonBytes, SimulatorRestJsonContext.Default.SimulatorRestMerchantDto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BetterRedsys Simulator: Could not deserialize Ds_MerchantParameters.");
            }
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        var html = GenerateSimulatorHtml(parameters, context.Request);
        await context.Response.WriteAsync(html);
    }

    private async Task RenderMockLandingUiAsync(HttpContext context)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        var html = GenerateSimulatorLandingHtml();
        await context.Response.WriteAsync(html);
    }

    private async Task HandleDispatchWebhookAsync(HttpContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            var simRequest = JsonSerializer.Deserialize(body, SimulatorJsonContext.Default.SimulatorSimulationRequest);

            if (simRequest is null || string.IsNullOrWhiteSpace(simRequest.Order))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errResponse = new SimulatorSimulationResponse(false, 400, null, null, "Invalid simulation request payload.");
                await context.Response.WriteAsJsonAsync(errResponse, SimulatorJsonContext.Default.SimulatorSimulationResponse);
                return;
            }

            // Construct simulated notification parameters
            var madridNow = RedsysTimeZone.Now();
            var now = madridNow.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            var hour = madridNow.ToString("HH:mm", CultureInfo.InvariantCulture);
            var isSuccess = RedsysResponseCatalog.IsSuccess(simRequest.ResponseCode);

            var notificationDto = new SimulatorRestNotificationDto
            {
                Date = Uri.EscapeDataString(now),
                Hour = Uri.EscapeDataString(hour),
                Amount = simRequest.Amount,
                Currency = simRequest.Currency ?? _redsysOptions.Currency,
                Order = simRequest.Order,
                MerchantCode = simRequest.MerchantCode ?? _redsysOptions.Fuc,
                Terminal = simRequest.Terminal ?? _redsysOptions.Terminal,
                Response = simRequest.ResponseCode,
                AuthorisationCode = isSuccess ? (simRequest.AuthorisationCode ?? "999888") : null,
                TransactionType = "0",
                SecurePayment = "1",
                CardCountry = "724",
                CardType = "D",
                CardBrand = "1",
                MerchantData = !string.IsNullOrWhiteSpace(simRequest.MerchantData) ? Uri.EscapeDataString(simRequest.MerchantData) : null,
                ProcessedPayMethod = simRequest.ProcessedPayMethod ?? "T"
            };

            // Serialize parameters
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(notificationDto, SimulatorRestJsonContext.Default.SimulatorRestNotificationDto);
            var notifParamsBase64 = Convert.ToBase64String(jsonBytes);

            // Compute authentic HMAC-SHA256 signature
            var diversifiedKey = _cryptoService.DiversifyKey(_redsysOptions.SecretKey, simRequest.Order);
            var signature = _cryptoService.ComputeSignature(notifParamsBase64, diversifiedKey);

            // Determine target webhook URL and apply SSRF protection
            var targetWebhookUrl = simRequest.MerchantUrl ?? _redsysOptions.MerchantUrl ?? string.Empty;
            if (string.IsNullOrWhiteSpace(targetWebhookUrl))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errResponse = new SimulatorSimulationResponse(false, 400, null, null, "No MerchantUrl notification endpoint configured.");
                await context.Response.WriteAsJsonAsync(errResponse, SimulatorJsonContext.Default.SimulatorSimulationResponse);
                return;
            }

            // Compose with local origin if relative, or validate host if absolute to prevent SSRF
            if (Uri.TryCreate(targetWebhookUrl, UriKind.Relative, out _))
            {
                var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
                targetWebhookUrl = $"{baseUrl}/{targetWebhookUrl.TrimStart('/')}";
            }
            else if (Uri.TryCreate(targetWebhookUrl, UriKind.Absolute, out var absoluteUri))
            {
                var isAllowedHost = string.Equals(absoluteUri.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(targetWebhookUrl, _redsysOptions.MerchantUrl, StringComparison.OrdinalIgnoreCase);

                if (!isAllowedHost)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    var errResponse = new SimulatorSimulationResponse(false, 400, null, null, "SSRF Protection: Webhook simulation cannot target unauthorized external hosts.");
                    await context.Response.WriteAsJsonAsync(errResponse, SimulatorJsonContext.Default.SimulatorSimulationResponse);
                    return;
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errResponse = new SimulatorSimulationResponse(false, 400, null, null, "Invalid webhook target URI format.");
                await context.Response.WriteAsJsonAsync(errResponse, SimulatorJsonContext.Default.SimulatorSimulationResponse);
                return;
            }

            // Dispatch HTTP POST to merchant webhook
            var httpClient = _httpClientFactory?.CreateClient("BetterRedsysSimulator") ?? SharedHttpClient;
            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [RedsysPaymentResponseData.FormFieldSignatureVersion] = "HMAC_SHA256_V1",
                [RedsysPaymentResponseData.FormFieldMerchantParameters] = notifParamsBase64,
                [RedsysPaymentResponseData.FormFieldSignature] = signature
            });

            _logger.LogInformation("BetterRedsys Simulator: Dispatching simulated webhook to {Url} with result {Code}", targetWebhookUrl, simRequest.ResponseCode);

            var response = await httpClient.PostAsync(targetWebhookUrl, formContent);
            var responseText = await response.Content.ReadAsStringAsync();

            var redirectUrl = isSuccess ? (simRequest.UrlOk ?? _redsysOptions.UrlOk ?? "/") : (simRequest.UrlKo ?? _redsysOptions.UrlKo ?? "/");

            var okResponse = new SimulatorSimulationResponse(
                Success: true,
                WebhookStatus: (int)response.StatusCode,
                WebhookResponse: responseText,
                RedirectUrl: redirectUrl);

            await context.Response.WriteAsJsonAsync(okResponse, SimulatorJsonContext.Default.SimulatorSimulationResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BetterRedsys Simulator: Error dispatching simulated webhook.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var errResponse = new SimulatorSimulationResponse(false, 500, null, null, ex.Message);
            await context.Response.WriteAsJsonAsync(errResponse, SimulatorJsonContext.Default.SimulatorSimulationResponse);
        }
    }

    private string GenerateSimulatorHtml(SimulatorRestMerchantDto? p, HttpRequest req)
    {
        var rawOrder = p?.Order ?? "000000";
        var rawAmount = p?.Amount ?? "0";
        var rawMerchantCode = p?.MerchantCode ?? _redsysOptions.Fuc;
        var rawTerminal = p?.Terminal ?? _redsysOptions.Terminal;
        var rawCurrency = p?.Currency ?? _redsysOptions.Currency;
        var rawMerchantUrl = p?.MerchantUrl ?? _redsysOptions.MerchantUrl ?? "";
        var rawUrlOk = p?.UrlOk ?? _redsysOptions.UrlOk ?? "";
        var rawUrlKo = p?.UrlKo ?? _redsysOptions.UrlKo ?? "";
        var rawMerchantData = p?.MerchantData ?? "";

        var initialData = new SimulatorSimulationInitialData(
            rawOrder,
            rawAmount,
            rawCurrency,
            rawMerchantCode,
            rawTerminal,
            rawMerchantUrl,
            rawUrlOk,
            rawUrlKo,
            rawMerchantData);

        var initialDataJson = JsonSerializer.Serialize(initialData, SimulatorJsonContext.Default.SimulatorSimulationInitialData);

        long.TryParse(rawAmount, out var cents);
        var amount = new RedsysAmount(cents);

        var order = WebUtility.HtmlEncode(rawOrder);
        var displayAmount = WebUtility.HtmlEncode(amount.ToDisplayString());
        var merchantCode = WebUtility.HtmlEncode(rawMerchantCode);
        var terminal = WebUtility.HtmlEncode(rawTerminal);
        _ = WebUtility.HtmlEncode(rawCurrency);
        var payMethods = WebUtility.HtmlEncode(p?.PayMethods ?? "Todos / All");
        var phone = WebUtility.HtmlEncode(p?.CustomerMobile ?? "(Not specified / No especificado)");
        var desc = WebUtility.HtmlEncode(p?.ProductDescription ?? "No description / Sin descripción");
        var merchantUrl = WebUtility.HtmlEncode(rawMerchantUrl);
        var urlOk = WebUtility.HtmlEncode(rawUrlOk);
        var urlKo = WebUtility.HtmlEncode(rawUrlKo);
        var consumerLangCode = WebUtility.HtmlEncode(p?.ConsumerLanguage ?? "001");
        var consumerLang = RedsysLanguageExtensions.FromRedsysCode(p?.ConsumerLanguage ?? "001");
        // No DS_MERCHANT_PAYMETHODS means every method enabled on the terminal, Bizum included
        var isBizumAvailable = string.IsNullOrWhiteSpace(p?.PayMethods) ||
                               p.PayMethods.Contains('z', StringComparison.OrdinalIgnoreCase);
        var bizumBadge = isBizumAvailable ? "<span class=\"bizum-pill\">Bizum</span>" : "";

        var dispatchUrl = $"{req.PathBase}{_simulatorOptions.Path.TrimEnd('/')}/dispatch-webhook";

        return SimulatorUiResources.RenderSimulatorHtml(
            amount: WebUtility.HtmlEncode(amount.ToDisplayString()),
            order: WebUtility.HtmlEncode(rawOrder),
            merchantCode: WebUtility.HtmlEncode(rawMerchantCode),
            terminal: WebUtility.HtmlEncode(rawTerminal),
            payMethods: WebUtility.HtmlEncode(p?.PayMethods ?? "Todos / All"),
            bizumBadge: bizumBadge,
            consumerLang: WebUtility.HtmlEncode(RedsysLanguageExtensions.FromRedsysCode(p?.ConsumerLanguage ?? "001").ToString()),
            consumerLangCode: WebUtility.HtmlEncode(p?.ConsumerLanguage ?? "001"),
            customerMobile: WebUtility.HtmlEncode(p?.CustomerMobile ?? "(Not specified / No especificado)"),
            description: WebUtility.HtmlEncode(p?.ProductDescription ?? "No description / Sin descripción"),
            merchantUrl: WebUtility.HtmlEncode(rawMerchantUrl),
            urlOk: WebUtility.HtmlEncode(rawUrlOk),
            urlKo: WebUtility.HtmlEncode(rawUrlKo),
            dispatchUrl: WebUtility.HtmlEncode(dispatchUrl),
            initialDataJson: initialDataJson);
    }

    private string GenerateSimulatorLandingHtml()
    {
        return SimulatorUiResources.RenderLandingHtml(WebUtility.HtmlEncode(_simulatorOptions.Path));
    }
}

/// <summary>
/// JSON initial order parameters embedded in simulator UI.
/// </summary>
internal sealed record SimulatorSimulationInitialData(
    [property: JsonPropertyName("order")] string? Order,
    [property: JsonPropertyName("amount")] string? Amount,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("merchantCode")] string? MerchantCode,
    [property: JsonPropertyName("terminal")] string? Terminal,
    [property: JsonPropertyName("merchantUrl")] string? MerchantUrl,
    [property: JsonPropertyName("urlOk")] string? UrlOk,
    [property: JsonPropertyName("urlKo")] string? UrlKo,
    [property: JsonPropertyName("merchantData")] string? MerchantData);

/// <summary>
/// Internal JSON simulation request model used by simulator.
/// </summary>
internal sealed record SimulatorSimulationRequest(
    [property: JsonPropertyName("order")] string? Order,
    [property: JsonPropertyName("amount")] string? Amount,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("merchantCode")] string? MerchantCode,
    [property: JsonPropertyName("terminal")] string? Terminal,
    [property: JsonPropertyName("merchantUrl")] string? MerchantUrl,
    [property: JsonPropertyName("urlOk")] string? UrlOk,
    [property: JsonPropertyName("urlKo")] string? UrlKo,
    [property: JsonPropertyName("merchantData")] string? MerchantData,
    [property: JsonPropertyName("responseCode")] string? ResponseCode,
    [property: JsonPropertyName("processedPayMethod")] string? ProcessedPayMethod,
    [property: JsonPropertyName("authorisationCode")] string? AuthorisationCode);

/// <summary>
/// JSON simulation response model returned by simulator upon dispatching a webhook.
/// </summary>
internal sealed record SimulatorSimulationResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("webhookStatus")] int WebhookStatus,
    [property: JsonPropertyName("webhookResponse")] string? WebhookResponse,
    [property: JsonPropertyName("redirectUrl")] string? RedirectUrl,
    [property: JsonPropertyName("error")] string? Error = null);

/// <summary>
/// Internal DTO for simulator REST requests.
/// </summary>
internal sealed class SimulatorRestRequestDto
{
    [JsonPropertyName("Ds_SignatureVersion")]
    public string? SignatureVersion { get; set; }

    [JsonPropertyName("Ds_MerchantParameters")]
    public string? MerchantParameters { get; set; }

    [JsonPropertyName("Ds_Signature")]
    public string? Signature { get; set; }
}

/// <summary>
/// Internal DTO for simulator REST responses.
/// </summary>
internal sealed class SimulatorRestResponseDto
{
    [JsonPropertyName("Ds_SignatureVersion")]
    public string? SignatureVersion { get; set; }

    [JsonPropertyName("Ds_MerchantParameters")]
    public string? MerchantParameters { get; set; }

    [JsonPropertyName("Ds_Signature")]
    public string? Signature { get; set; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Internal DTO for simulator merchant parameters.
/// </summary>
internal sealed class SimulatorRestMerchantDto
{
    [JsonPropertyName("DS_MERCHANT_AMOUNT")]
    public string? Amount { get; set; }

    [JsonPropertyName("DS_MERCHANT_ORDER")]
    public string? Order { get; set; }

    [JsonPropertyName("DS_MERCHANT_MERCHANTCODE")]
    public string? MerchantCode { get; set; }

    [JsonPropertyName("DS_MERCHANT_CURRENCY")]
    public string? Currency { get; set; }

    [JsonPropertyName("DS_MERCHANT_TRANSACTIONTYPE")]
    public string? TransactionType { get; set; }

    [JsonPropertyName("DS_MERCHANT_TERMINAL")]
    public string? Terminal { get; set; }

    [JsonPropertyName("DS_MERCHANT_MERCHANTURL")]
    public string? MerchantUrl { get; set; }

    [JsonPropertyName("DS_MERCHANT_URLOK")]
    public string? UrlOk { get; set; }

    [JsonPropertyName("DS_MERCHANT_URLKO")]
    public string? UrlKo { get; set; }

    [JsonPropertyName("DS_MERCHANT_PAYMETHODS")]
    public string? PayMethods { get; set; }

    [JsonPropertyName("DS_MERCHANT_CUSTOMER_MOBILE")]
    public string? CustomerMobile { get; set; }

    [JsonPropertyName("DS_MERCHANT_PRODUCTDESCRIPTION")]
    public string? ProductDescription { get; set; }

    [JsonPropertyName("DS_MERCHANT_MERCHANTDATA")]
    public string? MerchantData { get; set; }

    [JsonPropertyName("DS_MERCHANT_CONSUMERLANGUAGE")]
    public string? ConsumerLanguage { get; set; }

    [JsonPropertyName("DS_MERCHANT_IDENTIFIER")]
    public string? Identifier { get; set; }

    [JsonPropertyName("DS_MERCHANT_COF_INI")]
    public string? CofIni { get; set; }

    [JsonPropertyName("DS_MERCHANT_IDOPER")]
    public string? IdOper { get; set; }
}

/// <summary>
/// Internal DTO for simulator notification parameters.
/// </summary>
internal sealed class SimulatorRestNotificationDto
{
    [JsonPropertyName("Ds_Date")]
    public string? Date { get; set; }

    [JsonPropertyName("Ds_Hour")]
    public string? Hour { get; set; }

    [JsonPropertyName("Ds_Amount")]
    public string? Amount { get; set; }

    [JsonPropertyName("Ds_Currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("Ds_Order")]
    public string? Order { get; set; }

    [JsonPropertyName("Ds_MerchantCode")]
    public string? MerchantCode { get; set; }

    [JsonPropertyName("Ds_Terminal")]
    public string? Terminal { get; set; }

    [JsonPropertyName("Ds_Response")]
    public string? Response { get; set; }

    [JsonPropertyName("Ds_AuthorisationCode")]
    public string? AuthorisationCode { get; set; }

    [JsonPropertyName("Ds_TransactionType")]
    public string? TransactionType { get; set; }

    [JsonPropertyName("Ds_SecurePayment")]
    public string? SecurePayment { get; set; }

    [JsonPropertyName("Ds_Card_Country")]
    public string? CardCountry { get; set; }

    [JsonPropertyName("Ds_Card_Type")]
    public string? CardType { get; set; }

    [JsonPropertyName("Ds_Card_Brand")]
    public string? CardBrand { get; set; }

    [JsonPropertyName("Ds_MerchantData")]
    public string? MerchantData { get; set; }

    [JsonPropertyName("Ds_ProcessedPayMethod")]
    public string? ProcessedPayMethod { get; set; }

    [JsonPropertyName("Ds_Merchant_Identifier")]
    public string? MerchantIdentifier { get; set; }
}

/// <summary>
/// System.Text.Json context for simulator simulation (Native AOT ready).
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(SimulatorSimulationInitialData))]
[JsonSerializable(typeof(SimulatorSimulationRequest))]
[JsonSerializable(typeof(SimulatorSimulationResponse))]
internal sealed partial class SimulatorJsonContext : JsonSerializerContext;

/// <summary>
/// System.Text.Json context for simulator REST operations simulation (Native AOT ready).
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(SimulatorRestRequestDto))]
[JsonSerializable(typeof(SimulatorRestResponseDto))]
[JsonSerializable(typeof(SimulatorRestMerchantDto))]
[JsonSerializable(typeof(SimulatorRestNotificationDto))]
internal sealed partial class SimulatorRestJsonContext : JsonSerializerContext;
