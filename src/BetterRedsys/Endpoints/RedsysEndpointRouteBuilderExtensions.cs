using System.Diagnostics.CodeAnalysis;
using BetterRedsys.Models;
using BetterRedsys.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BetterRedsys.Endpoints;

/// <summary>
/// Extension methods for mapping Redsys webhook endpoints in ASP.NET Core Minimal APIs.
/// </summary>
public static class RedsysEndpointRouteBuilderExtensions
{
    /// <param name="endpoints">The endpoint route builder.</param>
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps an asynchronous webhook endpoint for Redsys online notifications.
        /// Automatically validates the cryptographic signature and delivers a strongly-typed <see cref="RedsysNotification"/> instance.
        /// Responds with HTTP 400 Bad Request if parameters are missing or the signature is invalid.
        /// </summary>
        /// <param name="pattern">The HTTP route pattern (e.g., "/api/payments/webhook").</param>
        /// <param name="handler">Business delegate that receives the clean and validated notification.</param>
        /// <returns>The configured <see cref="RouteHandlerBuilder"/>.</returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026:MembersAnnotatedWithRequiresUnreferencedCode",
            Justification = "The delegate signature is statically typed and known.")]
        [UnconditionalSuppressMessage("AOT", "IL3050:MembersAnnotatedWithRequiresDynamicCode",
            Justification = "Minimal APIs in .NET generate or support statically bound route delegates.")]
        public RouteHandlerBuilder MapRedsysWebhook(string pattern,
            Func<RedsysNotification, Task<IResult>> handler)
        {
            return endpoints.MapPost(pattern, async (HttpContext context, IRedsysClient client, CancellationToken ct) =>
                {
                    var (merchantParameters, signature) = await ExtractRedsysParametersAsync(context, ct);

                    if (string.IsNullOrWhiteSpace(merchantParameters) || string.IsNullOrWhiteSpace(signature))
                    {
                        return Results.BadRequest(new
                        {
                            error = "Required Redsys parameters not found.",
                            error_es = "Parámetros obligatorios de Redsys no encontrados.",
                            message = "The request must contain Ds_MerchantParameters and Ds_Signature."
                        });
                    }

                    if (!client.TryDecodeNotification(merchantParameters, signature, out var notification, out var errorMessage))
                    {
                        return Results.BadRequest(new
                        {
                            error = "Invalid Redsys cryptographic signature or corrupted data.",
                            error_es = "Firma de Redsys inválida o parámetros adulterados.",
                            detail = errorMessage
                        });
                    }

                    return await handler(notification!);
                })
                .DisableAntiforgery()
                .WithName("RedsysWebhookAsync")
                .WithDisplayName("Redsys Notification Webhook")
                .WithDescription("Receives and cryptographically verifies online Redsys and Bizum notifications.");
        }

        /// <summary>
        /// Maps a synchronous webhook endpoint for Redsys online notifications.
        /// </summary>
        /// <param name="pattern">The HTTP route pattern (e.g., "/api/payments/webhook").</param>
        /// <param name="handler">Synchronous business delegate that receives the clean and validated notification.</param>
        [UnconditionalSuppressMessage("Trimming", "IL2026:MembersAnnotatedWithRequiresUnreferencedCode",
            Justification = "The delegate signature is statically typed and known.")]
        [UnconditionalSuppressMessage("AOT", "IL3050:MembersAnnotatedWithRequiresDynamicCode",
            Justification = "Minimal APIs in .NET generate or support statically bound route delegates.")]
        public RouteHandlerBuilder MapRedsysWebhook(string pattern,
            Func<RedsysNotification, IResult> handler)
        {
            return endpoints.MapRedsysWebhook(pattern, notification => Task.FromResult(handler(notification)));
        }
    }

    /// <summary>
    /// Extracts Ds_MerchantParameters and Ds_Signature from the HTTP request,
    /// searching first in application/x-www-form-urlencoded body and falling back to query string if necessary.
    /// </summary>
    private static async Task<(string? MerchantParameters, string? Signature)> ExtractRedsysParametersAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        string? merchantParameters = null;
        string? signature = null;

        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync(cancellationToken);
            if (form.TryGetValue("Ds_MerchantParameters", out var formParams))
            {
                merchantParameters = formParams.ToString();
            }

            if (form.TryGetValue("Ds_Signature", out var formSig))
            {
                signature = formSig.ToString();
            }
        }

        // Query string fallback in case of GET redirections containing parameters
        if (string.IsNullOrWhiteSpace(merchantParameters) && context.Request.Query.TryGetValue("Ds_MerchantParameters", out var queryParams))
        {
            merchantParameters = queryParams.ToString();
        }

        if (string.IsNullOrWhiteSpace(signature) && context.Request.Query.TryGetValue("Ds_Signature", out var querySig))
        {
            signature = querySig.ToString();
        }

        return (merchantParameters, signature);
    }
}
