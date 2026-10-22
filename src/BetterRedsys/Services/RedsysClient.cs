using System.Net.Http.Headers;
using System.Text.Json;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.Models;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Services;

/// <summary>
/// Central client for creating and validating Redsys and Bizum payments as well as executing server-to-server REST operations.
/// </summary>
public sealed class RedsysClient(
    IOptions<RedsysOptions> options,
    IRedsysCryptoService cryptoService,
    IHttpClientFactory? httpClientFactory = null) : IRedsysClient
{
    private readonly RedsysOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));
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

        if (string.IsNullOrWhiteSpace(merchantParametersBase64))
        {
            errorMessage = "The Ds_MerchantParameters parameter is missing or empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            errorMessage = "The Ds_Signature parameter is missing or empty.";
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
                return false;
            }

            notification = RedsysNotification.Create(dto, merchantParametersBase64, signature);
            return true;
        }
        catch
        {
            errorMessage = "The notification payload is malformed or corrupted.";
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

    private async Task<RedsysNotification> SendRestRequestAsync(
        string order,
        RedsysAmount amount,
        string transactionType,
        string? identifier = null,
        string? cofIni = null,
        string? cofType = null,
        CancellationToken cancellationToken = default)
    {
        _options.Validate();
        RedsysPaymentBuilder.ValidateOrder(order);

        var merchantDto = new RedsysMerchantParametersDto
        {
            Amount = amount.ToRedsysString(),
            Order = order,
            MerchantCode = _options.Fuc,
            Currency = _options.Currency,
            TransactionType = transactionType,
            Terminal = _options.Terminal,
            Identifier = identifier,
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
        using var httpClient = httpClientFactory?.CreateClient("BetterRedsys") ?? new HttpClient();
        using var content = new ByteArrayContent(requestBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await httpClient.PostAsync(restUrl, content, cancellationToken);
        var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        var restResponse = JsonSerializer.Deserialize(responseBytes, RedsysJsonContext.Default.RedsysRestResponseDto);

        if (restResponse is null || string.IsNullOrWhiteSpace(restResponse.MerchantParameters) || string.IsNullOrWhiteSpace(restResponse.Signature))
        {
            var err = restResponse?.ErrorCode ?? "Empty or invalid response from Redsys REST endpoint.";
            throw new InvalidOperationException($"Redsys REST operation failed: {err}");
        }

        return DecodeNotification(restResponse.MerchantParameters, restResponse.Signature);
    }
}
