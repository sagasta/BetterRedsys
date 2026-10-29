using System.Text.Json.Serialization;

namespace BetterRedsys.Models;

/// <summary>
/// Standard challenge indicator preferences for EMV 3-D Secure (3DS2 / PSD2 SCA).
/// </summary>
public static class Redsys3DsChallengePreference
{
    /// <summary>
    /// No challenge preference specified ("01").
    /// </summary>
    public const string NoPreference = "01";

    /// <summary>
    /// Merchant prefers challenge / SCA authentication ("02").
    /// </summary>
    public const string ChallengePreferred = "02";

    /// <summary>
    /// Challenge / SCA authentication is mandated by regulatory requirements ("03").
    /// </summary>
    public const string ChallengeMandated = "03";

    /// <summary>
    /// Request SCA exemption for low-value transactions under 30 EUR ("04").
    /// </summary>
    public const string ExemptionLowValue = "04";

    /// <summary>
    /// Request SCA exemption based on Transaction Risk Analysis (TRA) ("05").
    /// </summary>
    public const string ExemptionRiskAnalysis = "05";
}

/// <summary>
/// EMV 3-D Secure (3DS2 / PSD2 SCA) metadata payload used to request exemptions or supply customer authentication context.
/// Maps to the DS_MERCHANT_EMV3DS object in Redsys SIS.
/// </summary>
public sealed class RedsysEmv3Ds
{
    /// <summary>
    /// Challenge indicator preference (e.g. <see cref="Redsys3DsChallengePreference.ExemptionLowValue"/>).
    /// </summary>
    [JsonPropertyName("threeDSRequestorChallengeInd")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ThreeDSRequestorChallengeInd { get; set; }

    /// <summary>
    /// Cardholder account email address.
    /// </summary>
    [JsonPropertyName("email")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }

    /// <summary>
    /// Cardholder full name.
    /// </summary>
    [JsonPropertyName("cardholderName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CardholderName { get; set; }

    /// <summary>
    /// Cardholder mobile phone number.
    /// </summary>
    [JsonPropertyName("mobilePhone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RedsysPhone? MobilePhone { get; set; }

    /// <summary>
    /// Cardholder home phone number.
    /// </summary>
    [JsonPropertyName("homePhone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RedsysPhone? HomePhone { get; set; }

    /// <summary>
    /// Cardholder work phone number.
    /// </summary>
    [JsonPropertyName("workPhone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RedsysPhone? WorkPhone { get; set; }

    /// <summary>
    /// Indicator for ship address usage ("01" = Ship address matches billing, "02" = Verified address on file, etc.).
    /// </summary>
    [JsonPropertyName("shipAddressUsageInd")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ShipAddressUsageInd { get; set; }

    /// <summary>
    /// Cardholder account age indicator ("01" = Guest / no account, "02" = Created during txn, "03" = &lt;30 days, "04" = 30-60 days, "05" = &gt;60 days).
    /// </summary>
    [JsonPropertyName("chAccAgeInd")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AccountAgeIndicator { get; set; }

    /// <summary>
    /// Number of purchases with this account in the last 6 months.
    /// </summary>
    [JsonPropertyName("nbPurchaseAccount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PurchasesInLastSixMonths { get; set; }

    /// <summary>
    /// Suspicious account activity indicator ("01" = No suspicious activity, "02" = Suspicious activity observed).
    /// </summary>
    [JsonPropertyName("suspiciousAccActivity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SuspiciousActivityIndicator { get; set; }

    /// <summary>
    /// Shallow copy so the payment builder can adjust indicators without mutating the caller's instance.
    /// </summary>
    internal RedsysEmv3Ds Clone() => (RedsysEmv3Ds)MemberwiseClone();
}

/// <summary>
/// Phone number details for EMV 3DS authentication.
/// </summary>
public sealed class RedsysPhone
{
    /// <summary>
    /// Country dialing code (e.g., "34" for Spain).
    /// </summary>
    [JsonPropertyName("cc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CountryCode { get; set; }

    /// <summary>
    /// Subscriber phone number.
    /// </summary>
    [JsonPropertyName("subscriber")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Subscriber { get; set; }
}
