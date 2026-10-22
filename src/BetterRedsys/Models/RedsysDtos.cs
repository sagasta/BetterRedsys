using System.Text.Json.Serialization;

namespace BetterRedsys.Models;

/// <summary>
/// Internal DTO with the exact wire property structure expected by Redsys for DS_MERCHANT_PARAMETERS in payment requests.
/// </summary>
internal sealed class RedsysMerchantParametersDto
{
    [JsonPropertyName("DS_MERCHANT_AMOUNT")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Amount { get; set; }

    [JsonPropertyName("DS_MERCHANT_ORDER")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Order { get; set; }

    [JsonPropertyName("DS_MERCHANT_MERCHANTCODE")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MerchantCode { get; set; }

    [JsonPropertyName("DS_MERCHANT_CURRENCY")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Currency { get; set; }

    [JsonPropertyName("DS_MERCHANT_TRANSACTIONTYPE")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TransactionType { get; set; }

    [JsonPropertyName("DS_MERCHANT_TERMINAL")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Terminal { get; set; }

    [JsonPropertyName("DS_MERCHANT_MERCHANTURL")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MerchantUrl { get; set; }

    [JsonPropertyName("DS_MERCHANT_URLOK")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UrlOk { get; set; }

    [JsonPropertyName("DS_MERCHANT_URLKO")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UrlKo { get; set; }

    [JsonPropertyName("DS_MERCHANT_PAYMETHODS")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PayMethods { get; set; }

    [JsonPropertyName("DS_MERCHANT_CUSTOMER_MOBILE")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CustomerMobile { get; set; }

    [JsonPropertyName("DS_MERCHANT_PRODUCTDESCRIPTION")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProductDescription { get; set; }

    [JsonPropertyName("DS_MERCHANT_TITULAR")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Titular { get; set; }

    [JsonPropertyName("DS_MERCHANT_MERCHANTDATA")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MerchantData { get; set; }

    [JsonPropertyName("DS_MERCHANT_CONSUMERLANGUAGE")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConsumerLanguage { get; set; }

    [JsonPropertyName("DS_MERCHANT_IDENTIFIER")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Identifier { get; set; }

    [JsonPropertyName("DS_MERCHANT_COF_INI")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CofIni { get; set; }

    [JsonPropertyName("DS_MERCHANT_COF_TYPE")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CofType { get; set; }

    [JsonPropertyName("DS_MERCHANT_IDOPER")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IdOper { get; set; }

    [JsonPropertyName("DS_MERCHANT_EMV3DS")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RedsysEmv3Ds? Emv3Ds { get; set; }
}

/// <summary>
/// Internal DTO with the wire property structure received from Redsys in notification webhooks or REST responses.
/// </summary>
internal sealed class RedsysNotificationParametersDto
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

    [JsonPropertyName("Ds_Paymethods")]
    public string? Paymethods { get; set; }

    [JsonPropertyName("Ds_ErrorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("Ds_Merchant_Identifier")]
    public string? MerchantIdentifier { get; set; }

    [JsonPropertyName("Ds_Card_Number")]
    public string? CardNumber { get; set; }

    [JsonPropertyName("Ds_ExpiryDate")]
    public string? ExpiryDate { get; set; }
}

/// <summary>
/// Request wrapper payload sent to Redsys REST endpoint (trataPeticionREST).
/// </summary>
internal sealed class RedsysRestRequestDto
{
    [JsonPropertyName("Ds_SignatureVersion")]
    public string SignatureVersion { get; set; } = "HMAC_SHA256_V1";

    [JsonPropertyName("Ds_MerchantParameters")]
    public string MerchantParameters { get; set; } = string.Empty;

    [JsonPropertyName("Ds_Signature")]
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// Response wrapper payload returned by Redsys REST endpoint (trataPeticionREST).
/// </summary>
internal sealed class RedsysRestResponseDto
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
