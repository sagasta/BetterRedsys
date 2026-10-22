using System.Text;
using System.Text.Json;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.DevMock;
using BetterRedsys.Models;
using BetterRedsys.Services;
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
            .WithInitialCardOnFile(RedsysCofType.Recurring)
            .ToFormData();

        string json = Encoding.UTF8.GetString(Convert.FromBase64String(data.MerchantParameters));
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

        string json = Encoding.UTF8.GetString(Convert.FromBase64String(data.MerchantParameters));
        Assert.Contains("\"DS_MERCHANT_IDENTIFIER\":\"TOKEN_ABC_999\"", json);
        Assert.Contains("\"DS_MERCHANT_COF_INI\":\"N\"", json);
        Assert.Contains("\"DS_MERCHANT_COF_TYPE\":\"C\"", json);
    }

    private async Task<IHost> BuildDevMockTestHostAsync()
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddBetterRedsys(options =>
                    {
                        options.Fuc = "999008881";
                        options.Terminal = "1";
                        options.SecretKey = SecretKey;
                        options.UseDevMock = true; // Route to local DevMock
                    });
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseRedsysDevMock();
                });
            });

        return await builder.StartAsync();
    }

    [Fact]
    public async Task ExecuteRefundAsync_ViaDevMock_ReturnsApprovedRefundNotification()
    {
        using var host = await BuildDevMockTestHostAsync();
        var testHttpClient = host.GetTestClient();

        var options = host.Services.GetRequiredService<IOptions<RedsysOptions>>();
        var crypto = host.Services.GetRequiredService<IRedsysCryptoService>();

        // Crear una factory que proporcione el test client de TestHost
        var client = new RedsysClient(options, crypto, new SimpleHttpClientFactory(testHttpClient));

        RedsysNotification result = await client.ExecuteRefundAsync("2026102201", 15.00m);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("0900", result.ResponseCode); // 0900 = Devolución autorizada
        Assert.Equal("2026102201", result.Order);
        Assert.Equal(1500, result.Amount.Cents);
    }

    [Fact]
    public async Task ConfirmPreauthorizationAsync_ViaDevMock_ReturnsApprovedNotification()
    {
        using var host = await BuildDevMockTestHostAsync();
        var testHttpClient = host.GetTestClient();

        var options = host.Services.GetRequiredService<IOptions<RedsysOptions>>();
        var crypto = host.Services.GetRequiredService<IRedsysCryptoService>();

        var client = new RedsysClient(options, crypto, new SimpleHttpClientFactory(testHttpClient));

        RedsysNotification result = await client.ConfirmPreauthorizationAsync("2026102202", 50.00m);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("0000", result.ResponseCode);
        Assert.Equal("2026102202", result.Order);
    }

    [Fact]
    public async Task CancelPreauthorizationAsync_ViaDevMock_ReturnsCancellationApprovedNotification()
    {
        using var host = await BuildDevMockTestHostAsync();
        var testHttpClient = host.GetTestClient();

        var options = host.Services.GetRequiredService<IOptions<RedsysOptions>>();
        var crypto = host.Services.GetRequiredService<IRedsysCryptoService>();

        var client = new RedsysClient(options, crypto, new SimpleHttpClientFactory(testHttpClient));

        RedsysNotification result = await client.CancelPreauthorizationAsync("2026102203", 50.00m);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("0400", result.ResponseCode); // 0400 = Anulación autorizada
        Assert.Equal("2026102203", result.Order);
    }

    [Fact]
    public async Task ExecuteStoredCardChargeAsync_ViaDevMock_ReturnsSuccessWithToken()
    {
        using var host = await BuildDevMockTestHostAsync();
        var testHttpClient = host.GetTestClient();

        var options = host.Services.GetRequiredService<IOptions<RedsysOptions>>();
        var crypto = host.Services.GetRequiredService<IRedsysCryptoService>();

        var client = new RedsysClient(options, crypto, new SimpleHttpClientFactory(testHttpClient));

        RedsysNotification result = await client.ExecuteStoredCardChargeAsync("2026102204", 9.99m, "TOK_USER_555");

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("0000", result.ResponseCode);
        Assert.Equal("2026102204", result.Order);
        Assert.Equal(999, result.Amount.Cents);
    }

    private sealed class SimpleHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
