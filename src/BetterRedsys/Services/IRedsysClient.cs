using BetterRedsys.Models;

namespace BetterRedsys.Services;

/// <summary>
/// Primary client for creating, orchestrating, and validating Redsys and Bizum payments.
/// </summary>
public interface IRedsysClient
{
    /// <summary>
    /// Initializes a fluent builder for configuring and generating a Redsys payment request.
    /// </summary>
    /// <param name="order">Unique merchant order identifier (between 4 and 12 alphanumeric characters, first 4 must be numeric digits).</param>
    /// <param name="amount">Strongly-typed monetary amount.</param>
    /// <returns>A fluent <see cref="RedsysPaymentBuilder"/>.</returns>
    RedsysPaymentBuilder CreatePayment(string order, RedsysAmount amount);

    /// <summary>
    /// Decodes and cryptographically verifies an incoming notification payload received from Redsys webhook.
    /// </summary>
    /// <param name="merchantParametersBase64">The Base64 Ds_MerchantParameters string.</param>
    /// <param name="signature">The received Ds_Signature string.</param>
    /// <returns>A validated strongly-typed <see cref="RedsysNotification"/> domain model.</returns>
    /// <exception cref="InvalidOperationException">Thrown if cryptographic verification fails or parameters cannot be parsed.</exception>
    RedsysNotification DecodeNotification(string merchantParametersBase64, string signature);

    /// <summary>
    /// Attempts to decode and verify an incoming Redsys notification without throwing exceptions.
    /// </summary>
    /// <param name="merchantParametersBase64">The Base64 Ds_MerchantParameters string.</param>
    /// <param name="signature">The received Ds_Signature string.</param>
    /// <param name="notification">Output notification model if verification succeeds.</param>
    /// <param name="errorMessage">Output error details if verification fails.</param>
    /// <returns>True if the signature and parameters are valid; otherwise false.</returns>
    bool TryDecodeNotification(
        string merchantParametersBase64,
        string signature,
        out RedsysNotification? notification,
        out string? errorMessage);

    /// <summary>
    /// Executes a direct server-to-server refund (reembolso) via Redsys REST API (trataPeticionREST) without user interaction.
    /// </summary>
    /// <param name="order">Unique order identifier for the refund transaction.</param>
    /// <param name="amount">Amount to be refunded.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validated <see cref="RedsysNotification"/> domain model containing the bank's response (code 0900 on success).</returns>
    Task<RedsysNotification> ExecuteRefundAsync(
        string order,
        RedsysAmount amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a direct server-to-server preauthorization confirmation via Redsys REST API (trataPeticionREST).
    /// </summary>
    /// <param name="order">Order identifier of the original preauthorized transaction.</param>
    /// <param name="amount">Amount to be confirmed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validated <see cref="RedsysNotification"/> domain model containing the bank's response.</returns>
    Task<RedsysNotification> ConfirmPreauthorizationAsync(
        string order,
        RedsysAmount amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a direct server-to-server preauthorization cancellation / release via Redsys REST API (trataPeticionREST).
    /// </summary>
    /// <param name="order">Order identifier of the original preauthorized transaction to be canceled.</param>
    /// <param name="amount">Amount of the preauthorization to be canceled.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validated <see cref="RedsysNotification"/> domain model containing the bank's response.</returns>
    Task<RedsysNotification> CancelPreauthorizationAsync(
        string order,
        RedsysAmount amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a direct server-to-server recurring or 1-click charge using a previously stored card token (COF).
    /// </summary>
    /// <param name="order">Unique order identifier for the recurring charge.</param>
    /// <param name="amount">Amount to be charged.</param>
    /// <param name="cardToken">The token returned in Ds_Merchant_Identifier from the initial authorization.</param>
    /// <param name="cofType">The type of Card-On-File charge (defaults to Recurring).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validated <see cref="RedsysNotification"/> domain model containing the bank's response.</returns>
    Task<RedsysNotification> ExecuteStoredCardChargeAsync(
        string order,
        RedsysAmount amount,
        string cardToken,
        RedsysCofType cofType = RedsysCofType.Recurring,
        CancellationToken cancellationToken = default);
}
