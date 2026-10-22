namespace BetterRedsys.Cryptography;

/// <summary>
/// Contract for official Redsys cryptographic operations (3DES CBC key diversification + HMAC-SHA256 signature).
/// </summary>
public interface IRedsysCryptoService
{
    /// <summary>
    /// Diversifies the merchant secret key for a specific order using 3DES in CBC mode with zero IV and zero-byte padding.
    /// </summary>
    /// <param name="secretKeyBase64">The merchant secret key in Base64 format configured in the Redsys administration portal.</param>
    /// <param name="order">The unique merchant order identifier (DS_MERCHANT_ORDER).</param>
    /// <returns>Diversified key bytes for the order.</returns>
    byte[] DiversifyKey(string secretKeyBase64, string order);

    /// <summary>
    /// Computes the HMAC-SHA256 signature over the Base64 merchant parameters string using the diversified key.
    /// </summary>
    /// <param name="merchantParametersBase64">Base64-encoded merchant parameters.</param>
    /// <param name="diversifiedKey">Diversified key bytes derived for the order.</param>
    /// <returns>Base64-encoded cryptographic signature string.</returns>
    string ComputeSignature(string merchantParametersBase64, byte[] diversifiedKey);

    /// <summary>
    /// Cryptographically validates the received signature against the secret key and order,
    /// preventing timing attacks and tolerating URL-safe Base64 variations.
    /// </summary>
    /// <param name="merchantParametersBase64">Received merchant parameters in Base64.</param>
    /// <param name="receivedSignature">Received signature string from the webhook.</param>
    /// <param name="secretKeyBase64">Configured merchant secret key in Base64.</param>
    /// <param name="order">Order identifier corresponding to the parameters.</param>
    /// <returns>True if the signature is valid and authentic; otherwise false.</returns>
    bool VerifySignature(string merchantParametersBase64, string receivedSignature, string secretKeyBase64, string order);

    /// <summary>
    /// Normalizes a Base64 string that may contain URL-safe characters ('-' or '_') and completes any missing '=' padding.
    /// </summary>
    /// <param name="base64">The Base64 string to normalize.</param>
    /// <returns>A standard Base64 string valid for Convert.FromBase64String.</returns>
    string NormalizeBase64(string base64);
}
