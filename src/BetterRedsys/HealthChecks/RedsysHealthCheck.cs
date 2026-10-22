using BetterRedsys.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BetterRedsys.HealthChecks;

/// <summary>
/// ASP.NET Core Health Check that verifies connectivity and reachability of the active Redsys payment gateway.
/// </summary>
public sealed class RedsysHealthCheck(
    IOptions<RedsysOptions> options,
    IHttpClientFactory httpClientFactory) : IHealthCheck
{
    private readonly RedsysOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var targetUrl = _options.GetPaymentGatewayUrl();

        try
        {
            using var httpClient = _httpClientFactory.CreateClient("BetterRedsys");

            // Probe the gateway with a lightweight GET or HEAD request
            using var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var isHealthy = (int)response.StatusCode < 500;

            var data = new Dictionary<string, object>
            {
                ["gateway_url"] = targetUrl,
                ["is_production"] = _options.IsProduction,
                ["status_code"] = (int)response.StatusCode
            };

            return isHealthy
                ? HealthCheckResult.Healthy($"Redsys payment gateway is reachable (HTTP {(int)response.StatusCode}).", data)
                : HealthCheckResult.Degraded($"Redsys payment gateway returned server error (HTTP {(int)response.StatusCode}).", null, data);
        }
        catch (Exception ex)
        {
            var data = new Dictionary<string, object>
            {
                ["gateway_url"] = targetUrl,
                ["is_production"] = _options.IsProduction
            };

            return HealthCheckResult.Unhealthy($"Failed to communicate with Redsys payment gateway: {ex.Message}", ex, data);
        }
    }
}
