using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;

namespace BetterRedsys.Models;

/// <summary>
/// Helper and validator for Redsys order identifiers (Ds_Merchant_Order).
/// Enforces official Redsys format rules: 4 to 12 alphanumeric ASCII characters,
/// where the first 4 characters must be strictly numeric digits.
/// </summary>
public static class RedsysOrderNumber
{
    private const string RandomSuffixAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>
    /// Generates a valid, cryptographically randomized 12-character Redsys order identifier.
    /// Format: Current UTC year and month in 4 digits (yyMM) followed by 8 random uppercase alphanumeric characters,
    /// giving 36^8 (~2.8 trillion) combinations per month so collisions are negligible at any realistic volume.
    /// Guarantees that the first 4 characters are numeric and the length is exactly 12 characters.
    /// </summary>
    /// <returns>A valid 12-character Redsys order string.</returns>
    public static string Generate()
    {
        var datePrefix = DateTime.UtcNow.ToString("yyMM", CultureInfo.InvariantCulture);
        var randomSuffix = RandomNumberGenerator.GetString(RandomSuffixAlphabet, 8);
        return $"{datePrefix}{randomSuffix}";
    }

    /// <summary>
    /// Determines whether the specified order identifier complies with official Redsys rules.
    /// </summary>
    /// <param name="order">The order identifier to check.</param>
    /// <param name="errorMessage">When invalid, receives an explanation of the validation failure.</param>
    /// <returns><c>true</c> if the order identifier is valid; otherwise, <c>false</c>.</returns>
    public static bool IsValid(string? order, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(order))
        {
            errorMessage = "Order identifier cannot be null, empty, or whitespace.";
            return false;
        }

        if (order.Length is < 4 or > 12)
        {
            errorMessage = $"Order identifier '{order}' has {order.Length} characters. Redsys requires between 4 and 12 characters.";
            return false;
        }

        for (var i = 0; i < 4; i++)
        {
            if (!char.IsAsciiDigit(order[i]))
            {
                errorMessage = $"The first 4 characters of order '{order}' must be numeric digits according to Redsys specifications.";
                return false;
            }
        }

        if (!order.All(char.IsAsciiLetterOrDigit))
        {
            errorMessage = $"Order identifier '{order}' can only contain alphanumeric ASCII characters (letters and digits).";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Validates the order identifier, throwing an <see cref="ArgumentException"/> if it does not comply with Redsys rules.
    /// </summary>
    /// <param name="order">The order identifier to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the order identifier is invalid.</exception>
    public static void Validate(string? order)
    {
        if (!IsValid(order, out var errorMessage))
        {
            throw new ArgumentException(errorMessage, nameof(order));
        }
    }
}
