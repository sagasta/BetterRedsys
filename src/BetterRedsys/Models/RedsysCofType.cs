namespace BetterRedsys.Models;

/// <summary>
/// EMVCo / PSD2 Card-On-File (COF) transaction types supported by Redsys for recurring payments and tokenization.
/// </summary>
public enum RedsysCofType
{
    /// <summary>
    /// Recurring subscription charges with regular intervals (code "R").
    /// </summary>
    Recurring,

    /// <summary>
    /// Installment payments or scheduled installments (code "I").
    /// </summary>
    Installment,

    /// <summary>
    /// Unscheduled merchant-initiated or customer-initiated charges, such as automatic top-ups (code "U").
    /// </summary>
    Unscheduled,

    /// <summary>
    /// Card on file saved for subsequent 1-click customer checkouts (code "C").
    /// </summary>
    OneClick
}

/// <summary>
/// Extension methods for <see cref="RedsysCofType"/>.
/// </summary>
public static class RedsysCofTypeExtensions
{
    /// <summary>
    /// Converts the enum to the single-letter code required by Redsys.
    /// </summary>
    public static string ToRedsysCode(this RedsysCofType cofType) => cofType switch
    {
        RedsysCofType.Recurring => "R",
        RedsysCofType.Installment => "I",
        RedsysCofType.Unscheduled => "U",
        RedsysCofType.OneClick => "C",
        _ => "R"
    };

    /// <summary>
    /// Parses a single-letter code into a <see cref="RedsysCofType"/>.
    /// </summary>
    public static RedsysCofType FromRedsysCode(string? code) => code?.ToUpperInvariant() switch
    {
        "R" => RedsysCofType.Recurring,
        "I" => RedsysCofType.Installment,
        "U" => RedsysCofType.Unscheduled,
        "C" => RedsysCofType.OneClick,
        _ => RedsysCofType.Recurring
    };
}
