namespace BetterRedsys.Models;

/// <summary>
/// Prepared and cryptographically signed data payload required to initialize a Redsys payment.
/// Designed to be consumed by decoupled front-ends (React, Next.js, Blazor, Angular, Mobile Apps)
/// or server-rendered HTML forms.
/// </summary>
/// <param name="SignatureVersion">The signature algorithm version (standard: "HMAC_SHA256_V1").</param>
/// <param name="MerchantParameters">Merchant parameters serialized as JSON and Base64-encoded.</param>
/// <param name="Signature">HMAC-SHA256 cryptographic signature computed with the diversified 3DES key.</param>
/// <param name="ActionUrl">Gateway destination endpoint (Redsys Test, Redsys Production, or DevMock).</param>
/// <param name="Order">Associated unique order identifier.</param>
/// <param name="Amount">Strongly-typed transaction amount.</param>
public sealed record RedsysPaymentResponseData(
    string SignatureVersion,
    string MerchantParameters,
    string Signature,
    string ActionUrl,
    string Order,
    RedsysAmount Amount)
{
    /// <summary>
    /// Standard HTML form field name for the signature version.
    /// </summary>
    public const string FormFieldSignatureVersion = "Ds_SignatureVersion";

    /// <summary>
    /// Standard HTML form field name for the Base64 merchant parameters.
    /// </summary>
    public const string FormFieldMerchantParameters = "Ds_MerchantParameters";

    /// <summary>
    /// Standard HTML form field name for the cryptographic signature.
    /// </summary>
    public const string FormFieldSignature = "Ds_Signature";
}
