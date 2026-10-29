using System.Globalization;

namespace BetterRedsys.Models;

/// <summary>
/// Strongly-typed domain model representing an online notification webhook dispatched by Redsys SIS.
/// </summary>
public sealed class RedsysNotification
{
    /// <summary>
    /// Indicates whether the transaction was approved and authorized by the cardholder's bank.
    /// </summary>
    public bool IsSuccess => RedsysResponseCatalog.IsSuccess(ResponseCode);

    /// <summary>
    /// Indicates whether the payment was completed using Bizum.
    /// </summary>
    public bool IsBizum =>
        string.Equals(ProcessedPayMethod, "z", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(ProcessedPayMethod, "bizum", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(PayMethods, "z", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Human-readable explanation of the payment result in Spanish (default Redsys response format).
    /// </summary>
    public string ResponseDescription => RedsysResponseCatalog.GetDescription(ResponseCode);

    /// <summary>
    /// Human-readable explanation of the payment result in English.
    /// </summary>
    public string ResponseDescriptionEn => RedsysResponseCatalog.GetDescription(ResponseCode, RedsysLanguage.English);

    /// <summary>
    /// Returns the localized description of the payment result in the specified language.
    /// </summary>
    /// <param name="language">The target language.</param>
    public string GetResponseDescription(RedsysLanguage language) =>
        RedsysResponseCatalog.GetDescription(ResponseCode, language);

    /// <summary>
    /// Returns the localized description of the payment result using a <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="culture">The target culture.</param>
    public string GetResponseDescription(CultureInfo? culture) =>
        RedsysResponseCatalog.GetDescription(ResponseCode, culture);

    /// <summary>
    /// Unique merchant order identifier (Ds_Order).
    /// </summary>
    public string Order { get; init; } = string.Empty;

    /// <summary>
    /// Charged monetary amount represented as a strongly-typed <see cref="RedsysAmount"/>.
    /// </summary>
    public RedsysAmount Amount { get; init; }

    /// <summary>
    /// ISO-4217 numeric currency code (e.g., "978" for EUR).
    /// </summary>
    public string Currency { get; init; } = "978";

    /// <summary>
    /// Gateway response code (Ds_Response, e.g., "0000", "0184", "0912").
    /// </summary>
    public string ResponseCode { get; init; } = string.Empty;

    /// <summary>
    /// Bank authorization code assigned to the successful transaction (Ds_AuthorisationCode).
    /// </summary>
    public string? AuthorisationCode { get; init; }

    /// <summary>
    /// Operation transaction type (Ds_TransactionType, e.g., "0" = Standard Authorization).
    /// </summary>
    public string? TransactionType { get; init; }

    /// <summary>
    /// Indicates whether the transaction was verified via 3D Secure / SCA.
    /// </summary>
    public bool SecurePayment { get; init; }

    /// <summary>
    /// Card issuer country code according to ISO-3166-1 (e.g., "724" for Spain).
    /// </summary>
    public string? CardCountry { get; init; }

    /// <summary>
    /// Card type: "C" for Credit, "D" for Debit.
    /// </summary>
    public string? CardType { get; init; }

    /// <summary>
    /// Card brand: "1" = VISA, "2" = Mastercard, etc.
    /// </summary>
    public string? CardBrand { get; init; }

    /// <summary>
    /// Optional merchant custom data returned intact by Redsys (Ds_MerchantData).
    /// </summary>
    public string? MerchantData { get; init; }

    /// <summary>
    /// Payment method actually used to process the operation (Ds_ProcessedPayMethod).
    /// </summary>
    public string? ProcessedPayMethod { get; init; }

    /// <summary>
    /// Allowed payment methods configured on the operation (Ds_Paymethods).
    /// </summary>
    public string? PayMethods { get; init; }

    /// <summary>
    /// Token or customer identifier for recurring / credential-on-file (COF) transactions.
    /// </summary>
    public string? MerchantIdentifier { get; init; }

    /// <summary>
    /// Convenience alias for <see cref="MerchantIdentifier"/>.
    /// </summary>
    public string? Identifier => MerchantIdentifier;

    /// <summary>
    /// Masked card number returned in tokenization responses (Ds_Card_Number).
    /// </summary>
    public string? CardNumber { get; init; }

    /// <summary>
    /// Card expiration date (YYMM) returned in tokenization responses (Ds_ExpiryDate).
    /// </summary>
    public string? ExpiryDate { get; init; }

    /// <summary>
    /// Exact transaction timestamp provided by the Redsys server.
    /// </summary>
    public DateTimeOffset? TransactionDateTime { get; init; }

    /// <summary>
    /// Merchant FUC code received in the notification payload.
    /// </summary>
    public string? MerchantCode { get; init; }

    /// <summary>
    /// Merchant terminal received in the notification payload.
    /// </summary>
    public string? Terminal { get; init; }

    /// <summary>
    /// Original raw Base64 string of merchant parameters received in the HTTP request.
    /// </summary>
    public string RawMerchantParameters { get; init; } = string.Empty;

    /// <summary>
    /// Original raw cryptographic signature string received in the HTTP request.
    /// </summary>
    public string RawSignature { get; init; } = string.Empty;

    /// <summary>
    /// Creates a domain notification model from the deserialized wire DTO.
    /// </summary>
    internal static RedsysNotification Create(
        RedsysNotificationParametersDto dto,
        string rawMerchantParameters,
        string rawSignature)
    {
        // Parse safe integer cents
        long cents = 0;
        if (!string.IsNullOrWhiteSpace(dto.Amount) && long.TryParse(dto.Amount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedCents))
        {
            cents = parsedCents;
        }

        // Parse date and time (Redsys reports Spanish peninsular time, not the server's local time)
        DateTimeOffset? dateTime = null;
        if (!string.IsNullOrWhiteSpace(dto.Date) && !string.IsNullOrWhiteSpace(dto.Hour))
        {
            var dateStr = $"{Uri.UnescapeDataString(dto.Date)} {Uri.UnescapeDataString(dto.Hour)}";
            if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDt))
            {
                dateTime = RedsysTimeZone.FromRedsysLocalTime(parsedDt);
            }
        }

        return new RedsysNotification
        {
            Order = dto.Order ?? string.Empty,
            Amount = new RedsysAmount(cents),
            Currency = dto.Currency ?? "978",
            ResponseCode = dto.Response ?? string.Empty,
            AuthorisationCode = dto.AuthorisationCode,
            TransactionType = dto.TransactionType,
            SecurePayment = dto.SecurePayment == "1",
            CardCountry = dto.CardCountry,
            CardType = dto.CardType,
            CardBrand = dto.CardBrand,
            MerchantData = dto.MerchantData != null ? Uri.UnescapeDataString(dto.MerchantData) : null,
            ProcessedPayMethod = dto.ProcessedPayMethod,
            PayMethods = dto.Paymethods,
            MerchantIdentifier = dto.MerchantIdentifier,
            CardNumber = dto.CardNumber,
            ExpiryDate = dto.ExpiryDate,
            TransactionDateTime = dateTime,
            MerchantCode = dto.MerchantCode,
            Terminal = dto.Terminal,
            RawMerchantParameters = rawMerchantParameters,
            RawSignature = rawSignature
        };
    }
}
