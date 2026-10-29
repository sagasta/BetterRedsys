using System.Security.Cryptography;
using BetterRedsys.Cryptography;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Configuration;

/// <summary>
/// Validates <see cref="RedsysOptions"/> at application startup using <c>ValidateOnStart()</c>.
/// Prevents runtime failure during live transaction processing if credentials were misconfigured.
/// </summary>
internal sealed class RedsysOptionsValidator : IValidateOptions<RedsysOptions>
{
    public ValidateOptionsResult Validate(string? name, RedsysOptions options)
    {
        var failures = GetFailures(options);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    /// <summary>
    /// Single source of truth for configuration rules, shared by startup validation and <see cref="RedsysOptions.Validate"/>.
    /// </summary>
    internal static List<string> GetFailures(RedsysOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Fuc))
        {
            failures.Add("Merchant FUC code (Fuc) is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Terminal))
        {
            failures.Add("Terminal number (Terminal) is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            failures.Add("Secret Base64 signature key (SecretKey) is required.");
        }
        else if (!IsValidSecretKey(options.SecretKey))
        {
            failures.Add("Secret signature key (SecretKey) must be a valid Base64 string decoding to a 16 or 24 byte 3DES key (Redsys keys are 32 Base64 characters).");
        }

        if (string.IsNullOrWhiteSpace(options.Currency))
        {
            failures.Add("Currency code (Currency) is required.");
        }

        return failures;
    }

    private static bool IsValidSecretKey(string secretKey)
    {
        var normalized = RedsysCryptoService.Normalize(secretKey);
        var buffer = new byte[normalized.Length];

        if (!Convert.TryFromBase64String(normalized, buffer, out var bytesWritten))
        {
            return false;
        }

        CryptographicOperations.ZeroMemory(buffer);
        return bytesWritten is 16 or 24;
    }
}
