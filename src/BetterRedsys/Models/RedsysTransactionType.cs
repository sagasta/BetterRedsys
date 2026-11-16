namespace BetterRedsys.Models;

/// <summary>
/// Redsys transaction types (DS_MERCHANT_TRANSACTIONTYPE) most commonly used from a merchant integration.
/// </summary>
public enum RedsysTransactionType
{
    /// <summary>
    /// Standard authorization (code "0").
    /// </summary>
    Authorization,

    /// <summary>
    /// Preauthorization: the amount is held on the card but not charged until confirmed (code "1").
    /// </summary>
    Preauthorization,

    /// <summary>
    /// Confirmation of a previous preauthorization (code "2").
    /// </summary>
    PreauthorizationConfirmation,

    /// <summary>
    /// Total or partial refund of a previous authorization (code "3").
    /// </summary>
    Refund,

    /// <summary>
    /// Cancellation / release of a previous preauthorization (code "9").
    /// </summary>
    PreauthorizationCancellation
}

/// <summary>
/// Extension methods for <see cref="RedsysTransactionType"/>.
/// </summary>
public static class RedsysTransactionTypeExtensions
{
    /// <summary>
    /// Converts the enum to the code required by Redsys in DS_MERCHANT_TRANSACTIONTYPE.
    /// </summary>
    public static string ToRedsysCode(this RedsysTransactionType transactionType) => transactionType switch
    {
        RedsysTransactionType.Authorization => "0",
        RedsysTransactionType.Preauthorization => "1",
        RedsysTransactionType.PreauthorizationConfirmation => "2",
        RedsysTransactionType.Refund => "3",
        RedsysTransactionType.PreauthorizationCancellation => "9",
        _ => throw new ArgumentOutOfRangeException(nameof(transactionType), transactionType, "Unknown Redsys transaction type.")
    };

    /// <summary>
    /// Parses a Redsys transaction type code (as received in Ds_TransactionType).
    /// </summary>
    /// <param name="code">The Redsys code (e.g. "0", "3").</param>
    /// <param name="transactionType">The parsed transaction type when recognized.</param>
    /// <returns>True when the code maps to a known <see cref="RedsysTransactionType"/>.</returns>
    public static bool TryParseRedsysCode(string? code, out RedsysTransactionType transactionType)
    {
        switch (code?.Trim())
        {
            case "0": transactionType = RedsysTransactionType.Authorization; return true;
            case "1": transactionType = RedsysTransactionType.Preauthorization; return true;
            case "2": transactionType = RedsysTransactionType.PreauthorizationConfirmation; return true;
            case "3": transactionType = RedsysTransactionType.Refund; return true;
            case "9": transactionType = RedsysTransactionType.PreauthorizationCancellation; return true;
            default: transactionType = default; return false;
        }
    }
}
