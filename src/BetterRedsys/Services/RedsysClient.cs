using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.Diagnostics;
using BetterRedsys.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Services;

/// <summary>
/// Central client for creating and validating Redsys and Bizum payments as well as executing server-to-server REST operations.
/// Fully instrumented with OpenTelemetry distributed tracing, metrics, and structured audit logs.
/// </summary>
public sealed class RedsysClient(
    IOptions<RedsysOptions> options,
    IRedsysCryptoService cryptoService,
    IHttpClientFactory? httpClientFactory = null,
    ILogger<RedsysClient>? logger = null) : IRedsysClient
{
    // Fallback for manual construction without IHttpClientFactory; shared to avoid socket exhaustion from per-request clients
    private static readonly HttpClient SharedHttpClient = new();

    private readonly RedsysOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly IRedsysCryptoService _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));

    /// <inheritdoc />
    public RedsysPaymentBuilder CreatePayment(string order, RedsysAmount amount)
    {
        return new RedsysPaymentBuilder(order, amount, _options, _cryptoService);
    }

    /// <inheritdoc />
    public RedsysNotification DecodeNotification(string merchantParametersBase64, string signature)
    {
        if (!TryDecodeNotification(merchantParametersBase64, signature, out var notification, out var errorMessage))
        {
            throw new InvalidOperationException($"Error validating Redsys notification: {errorMessage}");
        }

        return notification!;
    }

    /// <inheritdoc />
    public bool TryDecodeNotification(
        string merchantParametersBase64,
        string signature,
        out RedsysNotification? notification,
        out string? errorMessage)
    {
        notification = null;
        errorMessage = null;

        using var activity = RedsysTelemetry.ActivitySource.StartActivity("Redsys.VerifyWebhook");

        if (string.IsNullOrWhiteSpace(merchantParametersBase64))
        {
            errorMessage = "The Ds_MerchantParameters parameter is missing or empty.";
            activity?.SetTag("redsys.error", errorMessage);
            return false;
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            errorMessage = "The Ds_Signature parameter is missing or empty.";
            activity?.SetTag("redsys.error", errorMessage);
            return false;
        }

        try
        {
            // Decode Base64 tolerating URL-safe variants
            var normalizedParams = _cryptoService.NormalizeBase64(merchantParametersBase64);
            var jsonBytes = Convert.FromBase64String(normalizedParams);

            // Deserialize using System.Text.Json Source Generator (Native AOT)
            var dto = JsonSerializer.Deserialize(jsonBytes, RedsysJsonContext.Default.RedsysNotificationParametersDto);

            if (dto is null || string.IsNullOrWhiteSpace(dto.Order))
            {
                errorMessage = "Could not extract the order identifier (Ds_Order) from the received parameters.";
                activity?.SetTag("redsys.error", errorMessage);
                return false;
            }

            // Verify cryptographic signature
            var isValid = _cryptoService.VerifySignature(
                merchantParametersBase64: merchantParametersBase64,
                receivedSignature: signature,
                secretKeyBase64: _options.SecretKey,
                order: dto.Order);

            if (!isValid)
            {
                errorMessage = "The cryptographic signature (Ds_Signature) does not match the received parameters.";
                activity?.SetTag("redsys.is_valid", false);
                activity?.SetTag("redsys.order", dto.Order);

                RedsysTelemetry.WebhooksReceivedCounter.Add(1,
                    new KeyValuePair<string, object?>("status", "invalid_signature"),
                    new KeyValuePair<string, object?>("is_bizum", false));

                logger?.LogWarning("BetterRedsys: Potential tampering detected. Cryptographic signature mismatch for order {Order}.", dto.Order);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(dto.MerchantCode) && !IsSameMerchantCode(dto.MerchantCode, _options.Fuc))
            {
                errorMessage = "The merchant code (Ds_MerchantCode) does not match the configured FUC.";
                activity?.SetTag("redsys.is_valid", false);
                activity?.SetTag("redsys.order", dto.Order);

                RedsysTelemetry.WebhooksReceivedCounter.Add(1,
                    new KeyValuePair<string, object?>("status", "merchant_mismatch"),
                    new KeyValuePair<string, object?>("is_bizum", false));

                logger?.LogWarning("BetterRedsys: Notification for order {Order} belongs to merchant {MerchantCode}, not the configured FUC.", dto.Order, dto.MerchantCode);
                return false;
            }

            notification = RedsysNotification.Create(dto, merchantParametersBase64, signature);

            activity?.SetTag("redsys.is_valid", true);
            activity?.SetTag("redsys.order", notification.Order);
            activity?.SetTag("redsys.amount", notification.Amount.ToDecimal());
            activity?.SetTag("redsys.response_code", notification.ResponseCode);
            activity?.SetTag("redsys.is_success", notification.IsSuccess);
            activity?.SetTag("redsys.is_bizum", notification.IsBizum);

            RedsysTelemetry.WebhooksReceivedCounter.Add(1,
                new KeyValuePair<string, object?>("status", notification.IsSuccess ? "authorized" : "rejected"),
                new KeyValuePair<string, object?>("is_bizum", notification.IsBizum));

            logger?.LogInformation(
                "BetterRedsys: Webhook verified for order {Order}. Response: {ResponseCode} ({Description}), Bizum: {IsBizum}.",
                notification.Order, notification.ResponseCode, notification.ResponseDescription, notification.IsBizum);

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = "The notification payload is malformed or corrupted.";
            activity?.SetTag("redsys.error", errorMessage);
            logger?.LogError(ex, "BetterRedsys: Failed to decode incoming notification.");
            return false;
        }
    }

    /// <inheritdoc />
    public Task<RedsysNotification> ExecuteRefundAsync(
        string order,
        RedsysAmount amount,
        CancellationToken cancellationToken = default)
    {
        return SendRestRequestAsync(
            order: order,
            amount: amount,
            transactionType: "3", // 3 = Refund / Devolución
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<RedsysNotification> ConfirmPreauthorizationAsync(
        string order,
        RedsysAmount amount,
        CancellationToken cancellationToken = default)
    {
        return SendRestRequestAsync(
            order: order,
            amount: amount,
            transactionType: "2", // 2 = Preauthorization Confirmation
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<RedsysNotification> CancelPreauthorizationAsync(
        string order,
        RedsysAmount amount,
        CancellationToken cancellationToken = default)
    {
        return SendRestRequestAsync(
            order: order,
            amount: amount,
            transactionType: "9", // 9 = Preauthorization Cancellation
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<RedsysNotification> ExecuteStoredCardChargeAsync(
        string order,
        RedsysAmount amount,
        string cardToken,
        RedsysCofType cofType = RedsysCofType.Recurring,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardToken);

        return SendRestRequestAsync(
            order: order,
            amount: amount,
            transactionType: "0", // 0 = Standard Authorization
            identifier: cardToken,
            cofIni: "N",
            cofType: cofType.ToRedsysCode(),
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<RedsysNotification> ExecuteInsitePaymentAsync(
        string order,
        RedsysAmount amount,
        string idOper,
        RedsysCofType? registerCardOnFile = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idOper);

        string? cofIni = registerCardOnFile.HasValue ? "S" : null;
        string? cofType = registerCardOnFile?.ToRedsysCode();

        return SendRestRequestAsync(
            order: order,
            amount: amount,
            transactionType: "0", // 0 = Standard Authorization
            idOper: idOper,
            cofIni: cofIni,
            cofType: cofType,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<RedsysNotification> QueryOrderAsync(
        string order,
        CancellationToken cancellationToken = default)
    {
        return SendRestRequestAsync(
            order: order,
            amount: 0.00m,
            transactionType: "J", // J = Consultation / Consulta de Operación
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Compares FUC codes ignoring surrounding whitespace and leading zeros, which Redsys may add or strip.
    /// </summary>
    private static bool IsSameMerchantCode(string received, string configured) =>
        string.Equals(
            received.Trim().TrimStart('0'),
            configured.Trim().TrimStart('0'),
            StringComparison.OrdinalIgnoreCase);

    private async Task<RedsysNotification> SendRestRequestAsync(
        string order,
        RedsysAmount amount,
        string transactionType,
        string? identifier = null,
        string? idOper = null,
        string? cofIni = null,
        string? cofType = null,
        CancellationToken cancellationToken = default)
    {
        _options.Validate();
        RedsysPaymentBuilder.ValidateOrder(order);

        var opName = transactionType switch
        {
            "3" => "refund",
            "2" => "preauth_confirm",
            "9" => "preauth_cancel",
            "J" => "order_query",
            _ => !string.IsNullOrWhiteSpace(idOper) ? "insite_auth" : "stored_card_charge"
        };

        using var activity = RedsysTelemetry.ActivitySource.StartActivity($"Redsys.Rest.{opName}");
        activity?.SetTag("redsys.order", order);
        activity?.SetTag("redsys.operation", opName);
        activity?.SetTag("redsys.amount", amount.ToDecimal());
        activity?.SetTag("redsys.currency", _options.Currency);

        logger?.LogInformation("BetterRedsys: Sending S2S REST {Operation} request for order {Order}, Amount {Amount}.", opName, order, amount.ToDisplayString());

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var merchantDto = new RedsysMerchantParametersDto
            {
                Amount = amount.ToRedsysString(),
                Order = order,
                MerchantCode = _options.Fuc,
                Currency = _options.Currency,
                TransactionType = transactionType,
                Terminal = _options.Terminal,
                Identifier = identifier,
                IdOper = idOper,
                CofIni = cofIni,
                CofType = cofType
            };

            var jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(merchantDto, RedsysJsonContext.Default.RedsysMerchantParametersDto);
            var merchantParametersBase64 = Convert.ToBase64String(jsonUtf8Bytes);

            var diversifiedKey = _cryptoService.DiversifyKey(_options.SecretKey, order);
            var signature = _cryptoService.ComputeSignature(merchantParametersBase64, diversifiedKey);

            var requestDto = new RedsysRestRequestDto
            {
                SignatureVersion = "HMAC_SHA256_V1",
                MerchantParameters = merchantParametersBase64,
                Signature = signature
            };

            var requestBytes = JsonSerializer.SerializeToUtf8Bytes(requestDto, RedsysJsonContext.Default.RedsysRestRequestDto);

            var restUrl = _options.GetRestGatewayUrl();
            var httpClient = httpClientFactory?.CreateClient("BetterRedsys") ?? SharedHttpClient;
            using var content = new ByteArrayContent(requestBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await httpClient.PostAsync(restUrl, content, cancellationToken);
            var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Redsys REST endpoint returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).",
                    inner: null,
                    statusCode: response.StatusCode);
            }

            var restResponse = JsonSerializer.Deserialize(responseBytes, RedsysJsonContext.Default.RedsysRestResponseDto);

            if (restResponse is null || string.IsNullOrWhiteSpace(restResponse.MerchantParameters) || string.IsNullOrWhiteSpace(restResponse.Signature))
            {
                var err = restResponse?.ErrorCode ?? "Empty or invalid response from Redsys REST endpoint.";
                throw new InvalidOperationException($"Redsys REST operation failed: {err}");
            }

            var notification = DecodeNotification(restResponse.MerchantParameters, restResponse.Signature);

            stopwatch.Stop();
            RedsysTelemetry.S2sDurationHistogram.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("operation", opName));

            RedsysTelemetry.S2sOperationsCounter.Add(1,
                new KeyValuePair<string, object?>("operation", opName),
                new KeyValuePair<string, object?>("is_success", notification.IsSuccess),
                new KeyValuePair<string, object?>("response_code", notification.ResponseCode));

            activity?.SetTag("redsys.response_code", notification.ResponseCode);
            activity?.SetTag("redsys.is_success", notification.IsSuccess);

            logger?.LogInformation("BetterRedsys: S2S REST {Operation} completed for order {Order} in {ElapsedMs}ms. ResponseCode: {ResponseCode}.",
                opName, order, stopwatch.ElapsedMilliseconds, notification.ResponseCode);

            return notification;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RedsysTelemetry.S2sDurationHistogram.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("operation", opName));

            RedsysTelemetry.S2sOperationsCounter.Add(1,
                new KeyValuePair<string, object?>("operation", opName),
                new KeyValuePair<string, object?>("is_success", false),
                new KeyValuePair<string, object?>("response_code", "error"));

            activity?.SetTag("redsys.is_success", false);
            logger?.LogError(ex, "BetterRedsys: S2S REST {Operation} failed for order {Order}.", opName, order);
            throw;
        }
    }
}
