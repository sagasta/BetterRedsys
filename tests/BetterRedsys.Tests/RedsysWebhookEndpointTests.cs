using System.Net;
using System.Text;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.Endpoints;
using BetterRedsys.Models;
using BetterRedsys.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BetterRedsys.Tests;

public class RedsysWebhookEndpointTests
{
    private const string SecretKey = RedsysTestCredentials.SecretKey;

    private async Task<IHost> BuildTestHostAsync(Action<RedsysNotification>? callback = null)
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
                    });
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapRedsysWebhook("/api/pago/notificacion", notification =>
                        {
                            callback?.Invoke(notification);
                            return Results.Ok(new { status = "processed", order = notification.Order });
                        });
                    });
                });
            });

        var host = await builder.StartAsync();
        return host;
    }

    [Fact]
    public async Task WebhookEndpoint_ValidPayload_Returns200AndInvokesHandler()
    {
        RedsysNotification? received = null;
        using var host = await BuildTestHostAsync(n => received = n);
        var client = host.GetTestClient();

        var order = "0001";
        var rawParamsJson = """
                            {
                                "Ds_Date": "22%2F10%2F2026",
                                "Ds_Hour": "16%3A00",
                                "Ds_Amount": "1999",
                                "Ds_Currency": "978",
                                "Ds_Order": "0001",
                                "Ds_MerchantCode": "999008881",
                                "Ds_Terminal": "001",
                                "Ds_Response": "0000",
                                "Ds_ProcessedPayMethod": "z"
                            }
                            """;

        var crypto = new RedsysCryptoService();
        var paramsBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawParamsJson));
        var key = crypto.DiversifyKey(SecretKey, order);
        var signature = crypto.ComputeSignature(paramsBase64, key);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Ds_SignatureVersion"] = "HMAC_SHA256_V1",
            ["Ds_MerchantParameters"] = paramsBase64,
            ["Ds_Signature"] = signature
        });

        var response = await client.PostAsync("/api/pago/notificacion", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(received);
        Assert.Equal("0001", received.Order);
        Assert.Equal(1999, received.Amount.Cents);
        Assert.True(received.IsSuccess);
        Assert.True(received.IsBizum);
    }

    [Fact]
    public async Task WebhookEndpoint_InvalidSignature_Returns400BadRequest()
    {
        var wasInvoked = false;
        using var host = await BuildTestHostAsync(_ => wasInvoked = true);
        var client = host.GetTestClient();

        var paramsBase64 = Convert.ToBase64String("{\"Ds_Order\":\"0001\",\"Ds_Response\":\"0000\"}"u8.ToArray());
        var fakeSignature = "SU5WQUxJRF9TSUdOQVRVUkU=";

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Ds_SignatureVersion"] = "HMAC_SHA256_V1",
            ["Ds_MerchantParameters"] = paramsBase64,
            ["Ds_Signature"] = fakeSignature
        });

        var response = await client.PostAsync("/api/pago/notificacion", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(wasInvoked);
    }

    [Fact]
    public async Task WebhookEndpoint_MissingParameters_Returns400BadRequest()
    {
        var wasInvoked = false;
        using var host = await BuildTestHostAsync(_ => wasInvoked = true);
        var client = host.GetTestClient();

        var form = new FormUrlEncodedContent(new Dictionary<string, string>());
        var response = await client.PostAsync("/api/pago/notificacion", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(wasInvoked);
    }
}
