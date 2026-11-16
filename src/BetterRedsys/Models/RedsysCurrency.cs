using System.Globalization;

namespace BetterRedsys.Models;

/// <summary>
/// ISO-4217 currencies with two decimal minor units commonly accepted by Redsys terminals.
/// The underlying value is the ISO-4217 numeric code. The terminal must have the currency enabled by the acquiring bank.
/// </summary>
public enum RedsysCurrency
{
    /// <summary>Euro (978).</summary>
    EUR = 978,

    /// <summary>US Dollar (840).</summary>
    USD = 840,

    /// <summary>Pound Sterling (826).</summary>
    GBP = 826,

    /// <summary>Swiss Franc (756).</summary>
    CHF = 756,

    /// <summary>Swedish Krona (752).</summary>
    SEK = 752,

    /// <summary>Norwegian Krone (578).</summary>
    NOK = 578,

    /// <summary>Danish Krone (208).</summary>
    DKK = 208,

    /// <summary>Polish Zloty (985).</summary>
    PLN = 985,

    /// <summary>Czech Koruna (203).</summary>
    CZK = 203,

    /// <summary>Canadian Dollar (124).</summary>
    CAD = 124,

    /// <summary>Australian Dollar (036).</summary>
    AUD = 36,

    /// <summary>Mexican Peso (484).</summary>
    MXN = 484,

    /// <summary>Brazilian Real (986).</summary>
    BRL = 986,

    /// <summary>Argentine Peso (032).</summary>
    ARS = 32,

    /// <summary>Moroccan Dirham (504).</summary>
    MAD = 504
}

/// <summary>
/// Extension methods for <see cref="RedsysCurrency"/>.
/// </summary>
public static class RedsysCurrencyExtensions
{
    /// <summary>
    /// Returns the three-digit ISO-4217 numeric code expected by Redsys (e.g. "978", "036").
    /// </summary>
    public static string ToIsoCode(this RedsysCurrency currency)
    {
        var code = (int)currency;
        if (code is <= 0 or > 999)
        {
            throw new ArgumentOutOfRangeException(nameof(currency), currency, "Invalid ISO-4217 numeric currency code.");
        }

        return code.ToString("D3", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses a three-digit ISO-4217 numeric code (as received in Ds_Currency) into a known <see cref="RedsysCurrency"/>.
    /// </summary>
    public static bool TryParseIsoCode(string? code, out RedsysCurrency currency)
    {
        if (int.TryParse(code, NumberStyles.None, CultureInfo.InvariantCulture, out var numeric) &&
            Enum.IsDefined((RedsysCurrency)numeric))
        {
            currency = (RedsysCurrency)numeric;
            return true;
        }

        currency = default;
        return false;
    }
}
