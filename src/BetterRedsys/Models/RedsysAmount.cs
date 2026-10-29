using System.Diagnostics;
using System.Globalization;

namespace BetterRedsys.Models;

/// <summary>
/// Immutable Value Object representing monetary amounts for Redsys in exact integer cents.
/// Eliminates rounding bugs and comma/period floating-point issues when communicating with the gateway.
/// </summary>
[DebuggerDisplay("{ToDisplayString(),nq}")]
public readonly record struct RedsysAmount : IComparable<RedsysAmount>, IComparable
{
    /// <summary>
    /// Total integer amount in cents (e.g., 15.50 € is represented as 1550).
    /// </summary>
    public long Cents { get; }

    /// <summary>
    /// Monetary value in main currency units (e.g., Euros) with decimal places.
    /// </summary>
    public decimal Euros => Cents / 100m;

    /// <summary>
    /// Converts this amount to a decimal in the main currency unit.
    /// </summary>
    public decimal ToDecimal() => Euros;

    /// <summary>
    /// Initializes a new instance of <see cref="RedsysAmount"/> from a total number of cents.
    /// </summary>
    /// <param name="cents">Total cents (must be non-negative).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when cents is negative.</exception>
    public RedsysAmount(long cents)
    {
        if (cents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cents), cents, "The amount cannot be negative.");
        }

        Cents = cents;
    }

    /// <summary>
    /// Creates a <see cref="RedsysAmount"/> from total cents.
    /// </summary>
    public static RedsysAmount FromCents(long cents) => new(cents);

    /// <summary>
    /// Creates a <see cref="RedsysAmount"/> from an amount in Euros or main currency unit (rounds to the nearest cent using midpoint away from zero).
    /// </summary>
    public static RedsysAmount FromEuros(decimal euros)
    {
        if (euros < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(euros), euros, "The amount cannot be negative.");
        }

        var cents = (long)Math.Round(euros * 100m, MidpointRounding.AwayFromZero);
        return new RedsysAmount(cents);
    }

    /// <summary>
    /// Creates a <see cref="RedsysAmount"/> from an integer number of Euros (e.g., 20 € -> 2000 cents).
    /// </summary>
    public static RedsysAmount FromEuros(int euros)
    {
        if (euros < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(euros), euros, "The amount cannot be negative.");
        }

        return new RedsysAmount((long)euros * 100);
    }

    /// <summary>
    /// Implicitly converts a decimal (assumed in main currency unit / Euros) to <see cref="RedsysAmount"/>.
    /// </summary>
    public static implicit operator RedsysAmount(decimal euros) => FromEuros(euros);

    /// <summary>
    /// Implicitly converts an integer (assumed in main currency unit / Euros) to <see cref="RedsysAmount"/>.
    /// </summary>
    public static implicit operator RedsysAmount(int euros) => FromEuros(euros);

    /// <summary>
    /// Implicitly converts a <see cref="RedsysAmount"/> to <see cref="decimal"/> in Euros.
    /// </summary>
    public static implicit operator decimal(RedsysAmount amount) => amount.Euros;

    /// <summary>
    /// Explicitly converts a <see cref="RedsysAmount"/> to <see cref="long"/> in cents.
    /// Explicit on purpose: an implicit cents conversion combined with the implicit euros conversion from
    /// <see cref="decimal"/> made expressions such as <c>amount * 2</c> silently multiply the amount by 100.
    /// </summary>
    public static explicit operator long(RedsysAmount amount) => amount.Cents;

    /// <summary>
    /// Returns the exact string required by Redsys (integer digits in cents without decimal separators).
    /// </summary>
    public string ToRedsysString() => Cents.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Friendly formatted string for display and debugging (e.g., "15.50 €").
    /// </summary>
    public string ToDisplayString(string currencySymbol = "€") =>
        $"{Euros.ToString("N2", CultureInfo.CurrentCulture)} {currencySymbol}".Trim();

    /// <inheritdoc />
    public override string ToString() => ToRedsysString();

    /// <summary>Compares two <see cref="RedsysAmount"/> instances.</summary>
    public int CompareTo(RedsysAmount other) => Cents.CompareTo(other.Cents);

    /// <summary>Compares this instance with another object.</summary>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is RedsysAmount other) return CompareTo(other);
        throw new ArgumentException("Object must be of type RedsysAmount.", nameof(obj));
    }

    /// <summary>Adds two amounts.</summary>
    public static RedsysAmount operator +(RedsysAmount a, RedsysAmount b) => new(a.Cents + b.Cents);

    /// <summary>Subtracts two amounts.</summary>
    public static RedsysAmount operator -(RedsysAmount a, RedsysAmount b) => new(a.Cents - b.Cents);

    /// <summary>Determines whether the first amount is less than the second.</summary>
    public static bool operator <(RedsysAmount a, RedsysAmount b) => a.Cents < b.Cents;

    /// <summary>Determines whether the first amount is less than or equal to the second.</summary>
    public static bool operator <=(RedsysAmount a, RedsysAmount b) => a.Cents <= b.Cents;

    /// <summary>Determines whether the first amount is greater than the second.</summary>
    public static bool operator >(RedsysAmount a, RedsysAmount b) => a.Cents > b.Cents;

    /// <summary>Determines whether the first amount is greater than or equal to the second.</summary>
    public static bool operator >=(RedsysAmount a, RedsysAmount b) => a.Cents >= b.Cents;

    /// <summary>
    /// Zero monetary amount.
    /// </summary>
    public static RedsysAmount Zero => new(0);
}
