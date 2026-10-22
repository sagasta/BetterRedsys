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

namespace BetterRedsys.DevMock;

/// <summary>
/// Development middleware for interactively simulating the Redsys gateway in-memory without ngrok or bank sandbox access.
/// </summary>
public sealed class RedsysDevMockMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RedsysOptions _options;
    private readonly IRedsysCryptoService _cryptoService;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<RedsysDevMockMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RedsysDevMockMiddleware"/>.
    /// </summary>
    public RedsysDevMockMiddleware(
        RequestDelegate next,
        IOptions<RedsysOptions> options,
        IRedsysCryptoService cryptoService,
        ILogger<RedsysDevMockMiddleware> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _next = next;
        _options = options.Value;
        _cryptoService = cryptoService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Processes incoming HTTP requests, intercepting calls directed to the Redsys simulator.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        string path = context.Request.Path.Value ?? string.Empty;
        string mockBasePath = _options.DevMockPath.TrimEnd('/');

        // Server-to-server REST simulation endpoint (trataPeticionREST)
        if (path.Equals(_options.DevMockRestPath, StringComparison.OrdinalIgnoreCase) &&
            HttpMethods.IsPost(context.Request.Method))
        {
            await HandleDevMockRestOperationAsync(context);
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

    private async Task HandleDevMockRestOperationAsync(HttpContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            string body = await reader.ReadToEndAsync();
            var restReq = JsonSerializer.Deserialize(body, RedsysJsonContext.Default.RedsysRestRequestDto);

            if (restReq is null || string.IsNullOrWhiteSpace(restReq.MerchantParameters))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            string normalizedParams = _cryptoService.NormalizeBase64(restReq.MerchantParameters);
            byte[] paramBytes = Convert.FromBase64String(normalizedParams);
            var merchantDto = JsonSerializer.Deserialize(paramBytes, RedsysJsonContext.Default.RedsysMerchantParametersDto);

            if (merchantDto is null || string.IsNullOrWhiteSpace(merchantDto.Order))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            // Determine response code based on transaction type
            string responseCode = merchantDto.TransactionType switch
            {
                "3" => "0900", // Refund approved
                "2" => "0000", // Preauthorization confirmed
                "9" => "0400", // Preauthorization canceled
                _ => "0000"    // Default direct authorization
            };

            var notificationDto = new RedsysNotificationParametersDto
            {
                Date = Uri.EscapeDataString(DateTime.Now.ToString("dd/MM/yyyy")),
                Hour = Uri.EscapeDataString(DateTime.Now.ToString("HH:mm")),
                Amount = merchantDto.Amount,
                Currency = merchantDto.Currency ?? _options.Currency,
                Order = merchantDto.Order,
                MerchantCode = merchantDto.MerchantCode ?? _options.Fuc,
                Terminal = merchantDto.Terminal ?? _options.Terminal,
                Response = responseCode,
                AuthorisationCode = "888999",
                TransactionType = merchantDto.TransactionType,
                SecurePayment = "1",
                CardCountry = "724",
                CardType = "D",
                CardBrand = "1",
                MerchantIdentifier = merchantDto.Identifier ?? "TOK_MOCK_123456789"
            };

            byte[] notifBytes = JsonSerializer.SerializeToUtf8Bytes(notificationDto, RedsysJsonContext.Default.RedsysNotificationParametersDto);
            string notifBase64 = Convert.ToBase64String(notifBytes);

            byte[] diversifiedKey = _cryptoService.DiversifyKey(_options.SecretKey, merchantDto.Order);
            string signature = _cryptoService.ComputeSignature(notifBase64, diversifiedKey);

            var restResponse = new RedsysRestResponseDto
            {
                SignatureVersion = "HMAC_SHA256_V1",
                MerchantParameters = notifBase64,
                Signature = signature
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(restResponse, RedsysJsonContext.Default.RedsysRestResponseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DevMock: Error processing REST simulation request.");
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

        RedsysMerchantParametersDto? parameters = null;
        if (!string.IsNullOrWhiteSpace(merchantParametersBase64))
        {
            try
            {
                string normalized = _cryptoService.NormalizeBase64(merchantParametersBase64);
                byte[] jsonBytes = Convert.FromBase64String(normalized);
                parameters = JsonSerializer.Deserialize(jsonBytes, RedsysJsonContext.Default.RedsysMerchantParametersDto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DevMock: Could not deserialize Ds_MerchantParameters.");
            }
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        string html = GenerateDevMockHtml(parameters, context.Request);
        await context.Response.WriteAsync(html);
    }

    private async Task RenderMockLandingUiAsync(HttpContext context)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        string html = GenerateDevMockLandingHtml();
        await context.Response.WriteAsync(html);
    }

    private async Task HandleDispatchWebhookAsync(HttpContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            string body = await reader.ReadToEndAsync();
            var simRequest = JsonSerializer.Deserialize(body, DevMockJsonContext.Default.DevMockSimulationRequest);

            if (simRequest is null || string.IsNullOrWhiteSpace(simRequest.Order))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errResponse = new DevMockSimulationResponse(false, 400, null, null, "Invalid simulation request payload.");
                await context.Response.WriteAsJsonAsync(errResponse, DevMockJsonContext.Default.DevMockSimulationResponse);
                return;
            }

            // Construct simulated notification parameters
            string now = DateTime.Now.ToString("dd/MM/yyyy");
            string hour = DateTime.Now.ToString("HH:mm");
            bool isSuccess = RedsysResponseCatalog.IsSuccess(simRequest.ResponseCode);

            var notificationDto = new RedsysNotificationParametersDto
            {
                Date = Uri.EscapeDataString(now),
                Hour = Uri.EscapeDataString(hour),
                Amount = simRequest.Amount,
                Currency = simRequest.Currency ?? _options.Currency,
                Order = simRequest.Order,
                MerchantCode = simRequest.MerchantCode ?? _options.Fuc,
                Terminal = simRequest.Terminal ?? _options.Terminal,
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
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(notificationDto, RedsysJsonContext.Default.RedsysNotificationParametersDto);
            string notifParamsBase64 = Convert.ToBase64String(jsonBytes);

            // Compute authentic HMAC-SHA256 signature
            byte[] diversifiedKey = _cryptoService.DiversifyKey(_options.SecretKey, simRequest.Order);
            string signature = _cryptoService.ComputeSignature(notifParamsBase64, diversifiedKey);

            // Determine target webhook URL
            string targetWebhookUrl = simRequest.MerchantUrl ?? _options.MerchantUrl ?? string.Empty;
            if (string.IsNullOrWhiteSpace(targetWebhookUrl))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errResponse = new DevMockSimulationResponse(false, 400, null, null, "No MerchantUrl notification endpoint configured.");
                await context.Response.WriteAsJsonAsync(errResponse, DevMockJsonContext.Default.DevMockSimulationResponse);
                return;
            }

            // Compose with local origin if relative
            if (Uri.TryCreate(targetWebhookUrl, UriKind.Relative, out _))
            {
                string baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
                targetWebhookUrl = $"{baseUrl}/{targetWebhookUrl.TrimStart('/')}";
            }

            // Dispatch HTTP POST to merchant webhook
            using var httpClient = _httpClientFactory?.CreateClient("BetterRedsysDevMock") ?? new HttpClient();
            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [RedsysPaymentResponseData.FormFieldSignatureVersion] = "HMAC_SHA256_V1",
                [RedsysPaymentResponseData.FormFieldMerchantParameters] = notifParamsBase64,
                [RedsysPaymentResponseData.FormFieldSignature] = signature
            });

            _logger.LogInformation("DevMock: Dispatching simulated webhook to {Url} with result {Code}", targetWebhookUrl, simRequest.ResponseCode);

            HttpResponseMessage response = await httpClient.PostAsync(targetWebhookUrl, formContent);
            string responseText = await response.Content.ReadAsStringAsync();

            string redirectUrl = isSuccess ? (simRequest.UrlOk ?? _options.UrlOk ?? "/") : (simRequest.UrlKo ?? _options.UrlKo ?? "/");

            var okResponse = new DevMockSimulationResponse(
                Success: true,
                WebhookStatus: (int)response.StatusCode,
                WebhookResponse: responseText,
                RedirectUrl: redirectUrl);

            await context.Response.WriteAsJsonAsync(okResponse, DevMockJsonContext.Default.DevMockSimulationResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DevMock: Error dispatching simulated webhook.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var errResponse = new DevMockSimulationResponse(false, 500, null, null, ex.Message);
            await context.Response.WriteAsJsonAsync(errResponse, DevMockJsonContext.Default.DevMockSimulationResponse);
        }
    }

    private string GenerateDevMockHtml(RedsysMerchantParametersDto? p, HttpRequest req)
    {
        string order = p?.Order ?? "000000";
        string rawAmount = p?.Amount ?? "0";
        long.TryParse(rawAmount, out long cents);
        var amount = new RedsysAmount(cents);
        string displayAmount = amount.ToDisplayString();
        string merchantCode = p?.MerchantCode ?? _options.Fuc;
        string terminal = p?.Terminal ?? _options.Terminal;
        string currency = p?.Currency ?? _options.Currency;
        string payMethods = p?.PayMethods ?? "Tarjeta / Card";
        string phone = p?.CustomerMobile ?? "(Not specified / No especificado)";
        string desc = p?.ProductDescription ?? "No description / Sin descripción";
        string merchantUrl = p?.MerchantUrl ?? _options.MerchantUrl ?? "";
        string urlOk = p?.UrlOk ?? _options.UrlOk ?? "";
        string urlKo = p?.UrlKo ?? _options.UrlKo ?? "";
        string merchantData = p?.MerchantData ?? "";
        string consumerLangCode = p?.ConsumerLanguage ?? "001";
        RedsysLanguage consumerLang = RedsysLanguageExtensions.FromRedsysCode(consumerLangCode);
        bool isBizumAvailable = payMethods.Contains('z', StringComparison.OrdinalIgnoreCase);

        string dispatchUrl = $"{req.PathBase}{_options.DevMockPath.TrimEnd('/')}/dispatch-webhook";

        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>BetterRedsys DevMock • Interactive Payment Simulator</title>
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
                        <h2>Redsys DevMock • Local Sandbox / Simulador Local</h2>
                        <p style="font-size: 0.85rem; color: var(--muted); margin-top: 4px;">In-memory payment simulation & webhook dispatcher • Simulación y despacho local.</p>
                    </div>
                    <div class="title-badge">
                        <span class="badge-dot"></span> DEV INTERCEPTOR
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

                    const payload = {
                        order: '{{order}}',
                        amount: '{{rawAmount}}',
                        currency: '{{currency}}',
                        merchantCode: '{{merchantCode}}',
                        terminal: '{{terminal}}',
                        merchantUrl: '{{merchantUrl}}',
                        urlOk: '{{urlOk}}',
                        urlKo: '{{urlKo}}',
                        merchantData: '{{merchantData}}',
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
                            
                            statusDetail.innerHTML = '<strong>Endpoint Response:</strong> ' + (data.webhookResponse || '(Empty body)') +
                                                     '<br><strong>Signature:</strong> Generated with official 3DES + HMAC-SHA256.';

                            redirectActions.style.display = 'block';
                            btnRedirect.href = data.redirectUrl;
                            btnRedirect.innerText = 'Navigate to ' + (payload.responseCode === '0000' ? 'URL OK' : 'URL KO') + ' (' + data.redirectUrl + ')';
                        } else {
                            statusHeader.innerHTML = '<span class="status-badge status-err">ERROR</span> Simulation error';
                            statusDetail.innerText = data.error || 'Unknown error';
                        }
                    } catch (err) {
                        btn.disabled = false;
                        btn.innerText = '🚀 Retry simulation / Reintentar';
                        statusHeader.innerHTML = '<span class="status-badge status-err">NETWORK ERROR</span>';
                        statusDetail.innerText = err.message;
                    }
                }
            </script>
        </body>
        </html>
        """;
    }

    private string GenerateDevMockLandingHtml()
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>BetterRedsys DevMock • Active / Activo</title>
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
                <div class="badge">● DEVMOCK ONLINE</div>
                <h1>BetterRedsys DevMock</h1>
                <p>The local Redsys & Bizum simulator is active and ready to intercept payment requests from your application.</p>
                <div class="code">
                    // Payments generated with RedsysPaymentBuilder<br>
                    // will be automatically intercepted at this route:<br>
                    Endpoint: {{_options.DevMockPath}}
                </div>
            </div>
        </body>
        </html>
        """;
    }
}

/// <summary>
/// Internal JSON simulation request model used by DevMock.
/// </summary>
public sealed record DevMockSimulationRequest(
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
/// JSON simulation response model returned by DevMock upon dispatching a webhook.
/// </summary>
public sealed record DevMockSimulationResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("webhookStatus")] int WebhookStatus,
    [property: JsonPropertyName("webhookResponse")] string? WebhookResponse,
    [property: JsonPropertyName("redirectUrl")] string? RedirectUrl,
    [property: JsonPropertyName("error")] string? Error = null);

/// <summary>
/// System.Text.Json context for DevMock simulation (Native AOT ready).
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(DevMockSimulationRequest))]
[JsonSerializable(typeof(DevMockSimulationResponse))]
internal sealed partial class DevMockJsonContext : JsonSerializerContext
{
}
