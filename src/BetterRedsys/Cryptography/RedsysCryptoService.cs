using System.Security.Cryptography;
using System.Text;

namespace BetterRedsys.Cryptography;

/// <summary>
/// High-performance and timing-safe cryptographic service for Redsys.
/// </summary>
public sealed class RedsysCryptoService : IRedsysCryptoService
{
    private static readonly byte[] ZeroIv = new byte[8];

    /// <inheritdoc />
    public byte[] DiversifyKey(string secretKeyBase64, string order)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKeyBase64);
        ArgumentException.ThrowIfNullOrWhiteSpace(order);

        string normalizedKey = NormalizeBase64(secretKeyBase64);
        byte[] keyBytes = Convert.FromBase64String(normalizedKey);

        byte[] orderBytes = Encoding.UTF8.GetBytes(order);

        // Redsys requires the order string to be padded with zeroes on the right
        // until its byte length is an exact multiple of 8 bytes (3DES block size).
        int remainder = orderBytes.Length % 8;
        byte[] paddedOrderBytes = orderBytes;
        if (remainder != 0)
        {
            paddedOrderBytes = new byte[orderBytes.Length + (8 - remainder)];
            Buffer.BlockCopy(orderBytes, 0, paddedOrderBytes, 0, orderBytes.Length);
        }

        using var tripleDes = TripleDES.Create();
        tripleDes.Key = keyBytes;
        tripleDes.Mode = CipherMode.CBC;
        tripleDes.Padding = PaddingMode.None; // Zero padding is manually performed above
        tripleDes.IV = ZeroIv;

        using var encryptor = tripleDes.CreateEncryptor();
        return encryptor.TransformFinalBlock(paddedOrderBytes, 0, paddedOrderBytes.Length);
    }

    /// <inheritdoc />
    public string ComputeSignature(string merchantParametersBase64, byte[] diversifiedKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantParametersBase64);
        ArgumentNullException.ThrowIfNull(diversifiedKey);

        using var hmac = new HMACSHA256(diversifiedKey);
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(merchantParametersBase64));
        return Convert.ToBase64String(hash);
    }

    /// <inheritdoc />
    public bool VerifySignature(string merchantParametersBase64, string receivedSignature, string secretKeyBase64, string order)
    {
        if (string.IsNullOrWhiteSpace(merchantParametersBase64) ||
            string.IsNullOrWhiteSpace(receivedSignature) ||
            string.IsNullOrWhiteSpace(secretKeyBase64) ||
            string.IsNullOrWhiteSpace(order))
        {
            return false;
        }

        try
        {
            byte[] diversifiedKey = DiversifyKey(secretKeyBase64, order);
            using var hmac = new HMACSHA256(diversifiedKey);
            byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(merchantParametersBase64));

            string normalizedReceived = NormalizeBase64(receivedSignature);
            byte[] receivedHash = Convert.FromBase64String(normalizedReceived);

            return CryptographicOperations.FixedTimeEquals(computedHash, receivedHash);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public string NormalizeBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return string.Empty;
        }

        // Handle URL-safe characters (- for + and _ for /)
        string normalized = base64.Replace('-', '+').Replace('_', '/');

        // Complete '=' padding if trimmed during HTTP transport
        int remainder = normalized.Length % 4;
        if (remainder > 0)
        {
            normalized = normalized.PadRight(normalized.Length + (4 - remainder), '=');
        }

        return normalized;
    }
}
