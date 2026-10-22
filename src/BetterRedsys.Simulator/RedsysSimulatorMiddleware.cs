using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Simulator;

/// <summary>
/// Development middleware for interactively simulating the Redsys gateway in-memory without ngrok or bank sandbox access.
/// </summary>
public sealed class RedsysSimulatorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RedsysOptions _redsysOptions;
    private readonly RedsysSimulatorOptions _simulatorOptions;
    private readonly IRedsysCryptoService _cryptoService;
    private readonly IHttpClientFactory? _httpClientFactory;
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
        IHttpClientFactory? httpClientFactory = null)
    {
        _next = next;
        _redsysOptions = redsysOptions.Value;
        _simulatorOptions = simulatorOptions.Value;
        _cryptoService = cryptoService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Processes incoming HTTP requests, intercepting calls directed to the Redsys simulator.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Production safety guard: do not intercept requests if running in Production
        if (_redsysOptions.IsProduction)
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
                Date = Uri.EscapeDataString(DateTime.Now.ToString("dd/MM/yyyy")),
                Hour = Uri.EscapeDataString(DateTime.Now.ToString("HH:mm")),
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
            var now = DateTime.Now.ToString("dd/MM/yyyy");
            var hour = DateTime.Now.ToString("HH:mm");
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
            using var httpClient = _httpClientFactory?.CreateClient("BetterRedsysSimulator") ?? new HttpClient();
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
        var payMethods = WebUtility.HtmlEncode(p?.PayMethods ?? "Tarjeta / Card");
        var phone = WebUtility.HtmlEncode(p?.CustomerMobile ?? "(Not specified / No especificado)");
        var desc = WebUtility.HtmlEncode(p?.ProductDescription ?? "No description / Sin descripción");
        var merchantUrl = WebUtility.HtmlEncode(rawMerchantUrl);
        var urlOk = WebUtility.HtmlEncode(rawUrlOk);
        var urlKo = WebUtility.HtmlEncode(rawUrlKo);
        var consumerLangCode = WebUtility.HtmlEncode(p?.ConsumerLanguage ?? "001");
        var consumerLang = RedsysLanguageExtensions.FromRedsysCode(p?.ConsumerLanguage ?? "001");
        var isBizumAvailable = (p?.PayMethods ?? "").Contains('z', StringComparison.OrdinalIgnoreCase);

        var dispatchUrl = WebUtility.HtmlEncode($"{req.PathBase}{_simulatorOptions.Path.TrimEnd('/')}/dispatch-webhook");

        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>BetterRedsys Simulator • Interactive Payment Simulator</title>
            <style>
                :root {
                    --bg: #090d16;
                    --card: #131b2e;
                    --card-border: rgba(255, 255, 255, 0.08);
                    --accent-red: #ef4444;
                    --accent-cyan: #06b6d4;
                    --accent-green: #10b981;
                    --accent-yellow: #f59e0b;
                    --text: #f1f5f9;
                    --muted: #94a3b8;
                    --input-bg: #1e293b;
                }
                * { box-sizing: border-box; margin: 0; padding: 0; }
                body {
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
                    background-color: var(--bg);
                    color: var(--text);
                    min-height: 100vh;
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                    padding: 32px 16px;
                }
                .container {
                    max-width: 860px;
                    width: 100%;
                    background: var(--card);
                    border: 1px solid var(--card-border);
                    border-radius: 20px;
                    box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.7);
                    overflow: hidden;
                }
                .header {
                    padding: 24px 32px;
                    background: linear-gradient(135deg, rgba(239, 68, 68, 0.15), rgba(6, 182, 212, 0.15));
                    border-bottom: 1px solid var(--card-border);
                    display: flex;
                    align-items: center;
                    justify-content: space-between;
                }
                .title-badge {
                    display: inline-flex;
                    align-items: center;
                    gap: 8px;
                    background: rgba(255, 255, 255, 0.08);
                    padding: 6px 12px;
                    border-radius: 9999px;
                    font-size: 0.75rem;
                    font-weight: 700;
                    letter-spacing: 0.05em;
                    text-transform: uppercase;
                }
                .badge-dot {
                    width: 8px;
                    height: 8px;
                    border-radius: 50%;
                    background: var(--accent-green);
                    box-shadow: 0 0 12px var(--accent-green);
                }
                .content { padding: 32px; display: grid; grid-template-columns: 1fr 1fr; gap: 32px; }
                @media (max-width: 768px) { .content { grid-template-columns: 1fr; } }
                .panel-title { font-size: 1rem; font-weight: 700; margin-bottom: 16px; display: flex; align-items: center; gap: 8px; }
                .info-grid { display: grid; gap: 10px; background: rgba(0,0,0,0.25); border-radius: 12px; padding: 16px; }
                .info-row { display: flex; justify-content: space-between; font-size: 0.85rem; border-bottom: 1px solid rgba(255,255,255,0.04); padding-bottom: 6px; }
                .info-row:last-child { border-bottom: none; padding-bottom: 0; }
                .info-label { color: var(--muted); }
                .info-val { font-weight: 600; text-align: right; }
                .amount-hero { font-size: 1.8rem; font-weight: 800; color: #fff; margin-bottom: 16px; }
                .form-group { margin-bottom: 16px; }
                label { display: block; font-size: 0.82rem; font-weight: 600; color: var(--muted); margin-bottom: 6px; }
                select, input {
                    width: 100%;
                    padding: 10px 14px;
                    background: var(--input-bg);
                    border: 1px solid var(--card-border);
                    border-radius: 8px;
                    color: var(--text);
                    font-size: 0.9rem;
                    outline: none;
                    transition: border 0.2s;
                }
                select:focus, input:focus { border-color: var(--accent-cyan); }
                .btn {
                    width: 100%;
                    padding: 12px 20px;
                    border: none;
                    border-radius: 10px;
                    font-size: 0.95rem;
                    font-weight: 700;
                    cursor: pointer;
                    display: inline-flex;
                    align-items: center;
                    justify-content: center;
                    gap: 8px;
                    transition: transform 0.15s, opacity 0.2s;
                }
                .btn:active { transform: scale(0.98); }
                .btn-simulate {
                    background: linear-gradient(135deg, #0ea5e9, #06b6d4);
                    color: #fff;
                    box-shadow: 0 10px 20px -5px rgba(6, 182, 212, 0.4);
                }
                .btn-redirect {
                    margin-top: 10px;
                    background: rgba(255,255,255,0.06);
                    color: var(--text);
                    border: 1px solid var(--card-border);
                    text-decoration: none;
                }
                .btn-redirect:hover { background: rgba(255,255,255,0.12); }
                .status-box {
                    margin-top: 20px;
                    background: rgba(0,0,0,0.35);
                    border-radius: 10px;
                    padding: 14px;
                    font-size: 0.82rem;
                    display: none;
                }
                .status-badge { display: inline-block; padding: 2px 8px; border-radius: 4px; font-weight: 700; }
                .status-200 { background: rgba(16, 185, 129, 0.2); color: var(--accent-green); }
                .status-err { background: rgba(239, 68, 68, 0.2); color: var(--accent-red); }
                .bizum-pill { background: rgba(6, 182, 212, 0.15); color: var(--accent-cyan); padding: 2px 8px; border-radius: 6px; font-weight: 600; font-size: 0.75rem; }
            </style>
        </head>
        <body>
            <div class="container">
                <div class="header">
                    <div>
                        <h2>BetterRedsys Simulator • Local Sandbox / Simulador Local</h2>
                        <p style="font-size: 0.85rem; color: var(--muted); margin-top: 4px;">In-memory payment simulation & webhook dispatcher • Simulación y despacho local.</p>
                    </div>
                    <div class="title-badge">
                        <span class="badge-dot"></span> SIMULATOR ACTIVE
                    </div>
                </div>

                <div class="content">
                    <div>
                        <div class="panel-title">📦 Order Data / Datos del Pedido</div>
                        <div class="amount-hero">{{displayAmount}}</div>

                        <div class="info-grid">
                            <div class="info-row"><span class="info-label">Order (Ds_Order):</span><span class="info-val">{{order}}</span></div>
                            <div class="info-row"><span class="info-label">Merchant (FUC):</span><span class="info-val">{{merchantCode}} (Terminal {{terminal}})</span></div>
                            <div class="info-row"><span class="info-label">Method / Método:</span><span class="info-val">{{payMethods}} {{(isBizumAvailable ? "<span class=\"bizum-pill\">Bizum</span>" : "")}}</span></div>
                            <div class="info-row"><span class="info-label">Language / Idioma:</span><span class="info-val">{{consumerLang}} ({{consumerLangCode}})</span></div>
                            <div class="info-row"><span class="info-label">Customer Mobile / Móvil:</span><span class="info-val">{{phone}}</span></div>
                            <div class="info-row"><span class="info-label">Description / Descripción:</span><span class="info-val">{{desc}}</span></div>
                            <div class="info-row"><span class="info-label">Webhook URL:</span><span class="info-val" style="font-family:monospace; font-size:0.75rem;">{{merchantUrl}}</span></div>
                            <div class="info-row"><span class="info-label">Return OK (UrlOk):</span><span class="info-val" style="font-family:monospace; font-size:0.75rem;">{{urlOk}}</span></div>
                            <div class="info-row"><span class="info-label">Return KO (UrlKo):</span><span class="info-val" style="font-family:monospace; font-size:0.75rem;">{{urlKo}}</span></div>
                        </div>
                    </div>

                    <div>
                        <div class="panel-title">⚡ Result Simulation / Simulación de Resultado</div>
                        
                        <div class="form-group">
                            <label for="selectResponse">Transaction Outcome / Resultado</label>
                            <select id="selectResponse">
                                <option value="0000" selected>🟢 0000 - Success / Transacción autorizada</option>
                                <option value="0116">🔴 0116 - Insufficient funds / Saldo insuficiente</option>
                                <option value="0180">🔴 0180 - Card expired or rejected / Tarjeta caducada</option>
                                <option value="0184">🔴 0184 - 3DS / SCA Authentication error / Fallo 3D Secure</option>
                                <option value="0190">🔴 0190 - Issuer declined / Denegación del emisor</option>
                                <option value="0912">🔴 0912 - Issuer host unavailable / Centro emisor no disponible</option>
                                <option value="9915">🟡 9915 - Canceled by user / Cancelado por el usuario</option>
                            </select>
                        </div>

                        <div class="form-group">
                            <label for="selectMethod">Payment Method Used / Método Empleado</label>
                            <select id="selectMethod">
                                <option value="T" selected>💳 Credit / Debit Card (Ds_ProcessedPayMethod = 'T')</option>
                                <option value="z">📱 Bizum (Ds_ProcessedPayMethod = 'z')</option>
                            </select>
                        </div>

                        <div class="form-group">
                            <label for="authCode">Authorization Code (Ds_AuthorisationCode)</label>
                            <input id="authCode" value="999888" placeholder="999888" />
                        </div>

                        <button id="btnSimulate" class="btn btn-simulate" onclick="simulatePayment()">
                            🚀 Simulate Payment & Dispatch Webhook / Simular Pago
                        </button>

                        <div id="statusBox" class="status-box">
                            <div style="font-weight: 700; margin-bottom: 6px;" id="statusHeader">Dispatching notification...</div>
                            <div id="statusDetail" style="color: var(--muted); line-height: 1.4;"></div>
                            <div id="redirectActions" style="margin-top: 12px; display: none;">
                                <a id="btnRedirect" class="btn btn-redirect" href="#">Browser Redirection / Volver a la tienda</a>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <script id="simulatorData" type="application/json">
                {{initialDataJson}}
            </script>
            <script>
                async function simulatePayment() {
                    const btn = document.getElementById('btnSimulate');
                    const statusBox = document.getElementById('statusBox');
                    const statusHeader = document.getElementById('statusHeader');
                    const statusDetail = document.getElementById('statusDetail');
                    const redirectActions = document.getElementById('redirectActions');
                    const btnRedirect = document.getElementById('btnRedirect');

                    btn.disabled = true;
                    btn.innerText = '⏳ Signing & dispatching webhook / Firmando webhook...';
                    statusBox.style.display = 'block';
                    redirectActions.style.display = 'none';

                    const baseData = JSON.parse(document.getElementById('simulatorData').textContent);
                    const payload = {
                        ...baseData,
                        responseCode: document.getElementById('selectResponse').value,
                        processedPayMethod: document.getElementById('selectMethod').value,
                        authorisationCode: document.getElementById('authCode').value
                    };

                    try {
                        const res = await fetch('{{dispatchUrl}}', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(payload)
                        });

                        const data = await res.json();
                        btn.disabled = false;
                        btn.innerText = '🚀 Simulate again / Simular de nuevo';

                        if (res.ok && data.success) {
                            const isHttpOk = data.webhookStatus >= 200 && data.webhookStatus < 300;
                            statusHeader.innerHTML = isHttpOk 
                                ? '<span class="status-badge status-200">HTTP ' + data.webhookStatus + ' OK</span> Webhook processed successfully / Webhook procesado con éxito' 
                                : '<span class="status-badge status-err">HTTP ' + data.webhookStatus + '</span> Webhook returned error / El webhook devolvió error';
                            
                            statusDetail.textContent = 'Endpoint Response: ' + (data.webhookResponse || '(Empty body)') + ' | Signature: Verified HMAC-SHA256.';

                            redirectActions.style.display = 'block';
                            btnRedirect.href = data.redirectUrl;
                            btnRedirect.innerText = 'Navigate to ' + (payload.responseCode === '0000' ? 'URL OK' : 'URL KO') + ' (' + data.redirectUrl + ')';
                        } else {
                            statusHeader.innerHTML = '<span class="status-badge status-err">ERROR</span> Simulation error';
                            statusDetail.textContent = data.error || 'Unknown error';
                        }
                    } catch (err) {
                        btn.disabled = false;
                        btn.innerText = '🚀 Retry simulation / Reintentar';
                        statusHeader.innerHTML = '<span class="status-badge status-err">NETWORK ERROR</span>';
                        statusDetail.textContent = err.message;
                    }
                }
            </script>
        </body>
        </html>
        """;
    }

    private string GenerateSimulatorLandingHtml()
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>BetterRedsys Simulator • Active / Activo</title>
            <style>
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #090d16; color: #f1f5f9; display: flex; align-items: center; justify-content: center; min-height: 100vh; margin: 0; padding: 20px; }
                .card { background: #131b2e; border: 1px solid rgba(255,255,255,0.08); border-radius: 16px; padding: 40px; max-width: 540px; text-align: center; box-shadow: 0 20px 40px rgba(0,0,0,0.5); }
                .badge { background: rgba(16,185,129,0.15); color: #10b981; border: 1px solid rgba(16,185,129,0.3); border-radius: 9999px; padding: 4px 12px; font-size: 0.8rem; font-weight: 700; display: inline-block; margin-bottom: 16px; }
                h1 { font-size: 1.5rem; margin-bottom: 12px; }
                p { color: #94a3b8; font-size: 0.95rem; line-height: 1.5; margin-bottom: 24px; }
                .code { background: rgba(0,0,0,0.3); border-radius: 8px; padding: 12px; font-family: monospace; font-size: 0.85rem; color: #06b6d4; text-align: left; }
            </style>
        </head>
        <body>
            <div class="card">
                <div class="badge">● SIMULATOR ONLINE</div>
                <h1>BetterRedsys Simulator</h1>
                <p>The local Redsys & Bizum simulator is active and ready to intercept payment requests from your application.</p>
                <div class="code">
                    // Payments generated with RedsysPaymentBuilder<br>
                    // will be automatically intercepted at this route:<br>
                    Endpoint: {{_simulatorOptions.Path}}
                </div>
            </div>
        </body>
        </html>
        """;
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
