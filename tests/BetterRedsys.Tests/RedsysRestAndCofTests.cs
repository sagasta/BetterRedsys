using System.Net;
using System.Text;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.Models;
using BetterRedsys.Services;
using BetterRedsys.Simulator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Tests;

public class RedsysRestAndCofTests
{
    private const string SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U";

    [Theory]
    [InlineData(RedsysCofType.Recurring, "R")]
    [InlineData(RedsysCofType.Installment, "I")]
    [InlineData(RedsysCofType.Unscheduled, "U")]
    [InlineData(RedsysCofType.OneClick, "C")]
    public void RedsysCofType_ToRedsysCode_ReturnsExpectedLetter(RedsysCofType cofType, string expected)
    {
        Assert.Equal(expected, cofType.ToRedsysCode());
        Assert.Equal(cofType, RedsysCofTypeExtensions.FromRedsysCode(expected));
    }

    [Fact]
    public void Builder_WithInitialCardOnFile_SetsCofFlagsInFormData()
    {
        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = SecretKey
        };
        var crypto = new RedsysCryptoService();
        var client = new RedsysClient(Options.Create(options), crypto);

        var data = client.CreatePayment("123456", 25.00m)
            .WithInitialCardOnFile()
            .ToFormData();

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(data.MerchantParameters));
        Assert.Contains("\"DS_MERCHANT_COF_INI\":\"S\"", json);
        Assert.Contains("\"DS_MERCHANT_COF_TYPE\":\"R\"", json);
    }

    [Fact]
    public void Builder_WithStoredCardCharge_SetsTokenAndFlagsInFormData()
    {
        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = SecretKey
        };
        var crypto = new RedsysCryptoService();
        var client = new RedsysClient(Options.Create(options), crypto);

        var data = client.CreatePayment("654321", 12.50m)
            .WithStoredCardCharge("TOKEN_ABC_999", RedsysCofType.OneClick)
            .ToFormData();

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(data.MerchantParameters));
        Assert.Contains("\"DS_MERCHANT_IDENTIFIER\":\"TOKEN_ABC_999\"", json);
        Assert.Contains("\"DS_MERCHANT_COF_INI\":\"N\"", json);
        Assert.Contains("\"DS_MERCHANT_COF_TYPE\":\"C\"", json);
    }

    private async Task<IHost> BuildSimulatorTestHostAsync()
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.UseEnvironment(Environments.Development);
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddBetterRedsys(options =>
                    {
                        options.Fuc = "999008881";
                        options.Terminal = "1";
                        options.SecretKey = SecretKey;
                    });
                    services.AddBetterRedsysSimulator();
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseRedsysSimulator();
                });
            });

        return await builder.StartAsync();
    }

    [Fact]
    public async Task ExecuteRefundAsync_ViaSimulator_ReturnsApprovedRefundNotification()
    {
        using var host = await BuildSimulatorTestHostAsync();
        var testHttpClient = host.GetTestClient();

        var options = host.Services.GetRequiredService<IOptions<RedsysOptions>>();
        var crypto = host.Services.GetRequiredService<IRedsysCryptoService>();

        // Crear una factory que proporcione el test client de TestHost
        var client = new RedsysClient(options, crypto, new SimpleHttpClientFactory(testHttpClient));

        var result = await client.ExecuteRefundAsync("2026102201", 15.00m);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("0900", result.ResponseCode); // 0900 = Devolución autorizada
        Assert.Equal("2026102201", result.Order);
        Assert.Equal(1500, result.Amount.Cents);
    }

    [Fact]
    public async Task ConfirmPreauthorizationAsync_ViaSimulator_ReturnsApprovedNotification()
    {
        using var host = await BuildSimulatorTestHostAsync();
        var testHttpClient = host.GetTestClient();

        var options = host.Services.GetRequiredService<IOptions<RedsysOptions>>();
        var crypto = host.Services.GetRequiredService<IRedsysCryptoService>();

        var client = new RedsysClient(options, crypto, new SimpleHttpClientFactory(testHttpClient));

        var result = await client.ConfirmPreauthorizationAsync("2026102202", 50.00m);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("0000", result.ResponseCode);
        Assert.Equal("2026102202", result.Order);
    }

    [Fact]
    public async Task CancelPreauthorizationAsync_ViaSimulator_ReturnsCancellationApprovedNotification()
    {
        using var host = await BuildSimulatorTestHostAsync();
        var testHttpClient = host.GetTestClient();

        var options = host.Services.GetRequiredService<IOptions<RedsysOptions>>();
        var crypto = host.Services.GetRequiredService<IRedsysCryptoService>();

        var client = new RedsysClient(options, crypto, new SimpleHttpClientFactory(testHttpClient));

        var result = await client.CancelPreauthorizationAsync("2026102203", 50.00m);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("0400", result.ResponseCode); // 0400 = Anulación autorizada
        Assert.Equal("2026102203", result.Order);
    }

    [Fact]
    public async Task ExecuteStoredCardChargeAsync_ViaSimulator_ReturnsSuccessWithToken()
    {
        using var host = await BuildSimulatorTestHostAsync();
        var testHttpClient = host.GetTestClient();

        var options = host.Services.GetRequiredService<IOptions<RedsysOptions>>();
        var crypto = host.Services.GetRequiredService<IRedsysCryptoService>();

        var client = new RedsysClient(options, crypto, new SimpleHttpClientFactory(testHttpClient));

        var result = await client.ExecuteStoredCardChargeAsync("2026102204", 9.99m, "TOK_USER_555");

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("0000", result.ResponseCode);
        Assert.Equal("2026102204", result.Order);
        Assert.Equal(999, result.Amount.Cents);
    }

    [Fact]
    public async Task QueryOrderAsync_ViaSimulator_ReturnsApprovedNotification()
    {
        using var host = await BuildSimulatorTestHostAsync();
        var testHttpClient = host.GetTestClient();

        var options = host.Services.GetRequiredService<IOptions<RedsysOptions>>();
        var crypto = host.Services.GetRequiredService<IRedsysCryptoService>();

        var client = new RedsysClient(options, crypto, new SimpleHttpClientFactory(testHttpClient));

        var result = await client.QueryOrderAsync("2026102205");

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("0000", result.ResponseCode);
        Assert.Equal("2026102205", result.Order);
    }

    [Fact]
    public async Task ExecuteInsitePaymentAsync_ViaSimulator_ReturnsApprovedWithToken()
    {
        using var host = await BuildSimulatorTestHostAsync();
        var testHttpClient = host.GetTestClient();

        var options = host.Services.GetRequiredService<IOptions<RedsysOptions>>();
        var crypto = host.Services.GetRequiredService<IRedsysCryptoService>();

        var client = new RedsysClient(options, crypto, new SimpleHttpClientFactory(testHttpClient));

        var result = await client.ExecuteInsitePaymentAsync(
            order: "2026102206",
            amount: 45.00m,
            idOper: "idOper_test_123456",
            registerCardOnFile: RedsysCofType.Recurring);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("0000", result.ResponseCode);
        Assert.Equal("2026102206", result.Order);
        Assert.Equal(4500, result.Amount.Cents);
        Assert.NotNull(result.MerchantIdentifier);
        Assert.StartsWith("TOK_SIM_", result.MerchantIdentifier);
    }

    [Fact]
    public void Builder_WithEmv3Ds_SerializesEmv3DsObjectCorrectly()
    {
        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = SecretKey
        };
        var crypto = new RedsysCryptoService();
        var client = new RedsysClient(Options.Create(options), crypto);

        var emv3Ds = new RedsysEmv3Ds
        {
            ThreeDSRequestorChallengeInd = Redsys3DsChallengePreference.ExemptionLowValue,
            Email = "customer@example.com",
            CardholderName = "Jane Doe",
            MobilePhone = new RedsysPhone { CountryCode = "34", Subscriber = "600123456" }
        };

        var data = client.CreatePayment("2026102207", 19.99m)
            .WithEmv3Ds(emv3Ds)
            .ToFormData();

        string json = Encoding.UTF8.GetString(Convert.FromBase64String(data.MerchantParameters));
        Assert.Contains("\"DS_MERCHANT_EMV3DS\":", json);
        Assert.Contains("\"threeDSRequestorChallengeInd\":\"04\"", json);
        Assert.Contains("\"email\":\"customer@example.com\"", json);
        Assert.Contains("\"subscriber\":\"600123456\"", json);
    }

    [Fact]
    public void Builder_With3DsExemption_SetsExemptionIndicator()
    {
        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = SecretKey
        };
        var crypto = new RedsysCryptoService();
        var client = new RedsysClient(Options.Create(options), crypto);

        var data = client.CreatePayment("2026102208", 15.00m)
            .With3DsExemption(Redsys3DsChallengePreference.ExemptionRiskAnalysis)
            .ToFormData();

        string json = Encoding.UTF8.GetString(Convert.FromBase64String(data.MerchantParameters));
        Assert.Contains("\"threeDSRequestorChallengeInd\":\"05\"", json);
    }

    private sealed class SimpleHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    [Fact]
    public async Task ExecuteRefundAsync_HttpErrorStatus_ThrowsHttpRequestExceptionWithStatus()
    {
        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = SecretKey,
            CustomRestGatewayUrl = "https://redsys.invalid/rest"
        };
        using var httpClient = new HttpClient(new StatusHttpMessageHandler(HttpStatusCode.BadGateway));
        var client = new RedsysClient(Options.Create(options), new RedsysCryptoService(), new SimpleHttpClientFactory(httpClient));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.ExecuteRefundAsync("2026102201", 10m));

        Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
        Assert.Contains("502", ex.Message);
    }

    private sealed class StatusHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent("<html>Bad Gateway</html>") });
    }
}
