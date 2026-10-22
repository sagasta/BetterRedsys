using System.Net;
using BetterRedsys.Configuration;
using BetterRedsys.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Tests;

public class RedsysHealthCheckTests
{
    [Fact]
    public async Task HealthCheck_WhenGatewayReturns200_ReportsHealthy()
    {
        var testHandler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(testHandler);
        var factory = new TestHttpClientFactory(httpClient);

        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
            CustomGatewayUrl = "https://mock.redsys.es/pay"
        };

        var check = new RedsysHealthCheck(Options.Create(options), factory);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal(200, (int)result.Data["status_code"]);
    }

    [Fact]
    public async Task HealthCheck_WhenGatewayReturns500_ReportsDegraded()
    {
        var testHandler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(testHandler);
        var factory = new TestHttpClientFactory(httpClient);

        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
            CustomGatewayUrl = "https://mock.redsys.es/pay"
        };

        var check = new RedsysHealthCheck(Options.Create(options), factory);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal(500, (int)result.Data["status_code"]);
    }

    [Fact]
    public async Task HealthCheck_WhenConnectionFails_ReportsUnhealthy()
    {
        var testHandler = new ThrowingHttpMessageHandler();
        var httpClient = new HttpClient(testHandler);
        var factory = new TestHttpClientFactory(httpClient);

        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
            CustomGatewayUrl = "https://unreachable.invalid/pay"
        };

        var check = new RedsysHealthCheck(Options.Create(options), factory);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public void AddRedsys_RegistersHealthCheckWithBuilder()
    {
        var services = new ServiceCollection();
        services.AddBetterRedsys(opt =>
        {
            opt.Fuc = "999008881";
            opt.Terminal = "1";
            opt.SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U";
        });
        services.AddHealthChecks().AddRedsys();

        var sp = services.BuildServiceProvider();
        var healthCheckOptions = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        var reg = healthCheckOptions.Registrations.FirstOrDefault(r => r.Name == "redsys");
        Assert.NotNull(reg);
        Assert.Contains("redsys", reg.Tags);
        Assert.Contains("payments", reg.Tags);
    }

    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class MockHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Simulated network outage");
        }
    }
}
