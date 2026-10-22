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

        if (string.IsNullOrWhiteSpace(options.Currency))
        {
            failures.Add("Currency code (Currency) is required.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
