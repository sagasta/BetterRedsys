using System.Text;
using System.Text.Json;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.DevMock;
using BetterRedsys.Models;
using BetterRedsys.Services;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Tests;

public class RedsysDevMockTests
{
    [Fact]
    public void GetPaymentGatewayUrl_UsesDevMockPath_WhenUseDevMockIsTrue()
    {
        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
            UseDevMock = true,
            DevMockPath = "/redsys-mock/pay"
        };

        Assert.Equal("/redsys-mock/pay", options.GetPaymentGatewayUrl());
    }

    [Fact]
    public void DevMock_SimulationParameters_AreVerifiableByClient()
    {
        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
            Currency = "978"
        };
        var crypto = new RedsysCryptoService();
        var client = new RedsysClient(Options.Create(options), crypto);

        string order = "2026102201";
        var simulatedDto = new RedsysNotificationParametersDto
        {
            Date = Uri.EscapeDataString("22/10/2026"),
            Hour = Uri.EscapeDataString("12:00"),
            Amount = "3500",
            Currency = "978",
            Order = order,
            MerchantCode = options.Fuc,
            Terminal = options.Terminal,
            Response = "0000",
            AuthorisationCode = "888777",
            TransactionType = "0",
            SecurePayment = "1",
            ProcessedPayMethod = "z"
        };

        // Simular lo que hace DevMock:
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(simulatedDto, RedsysJsonContext.Default.RedsysNotificationParametersDto);
        string paramsBase64 = Convert.ToBase64String(jsonBytes);

        byte[] diversifiedKey = crypto.DiversifyKey(options.SecretKey, order);
        string signature = crypto.ComputeSignature(paramsBase64, diversifiedKey);

        // Validar que RedsysClient decodifica y verifica exitosamente
        bool isValid = client.TryDecodeNotification(paramsBase64, signature, out var notification, out var error);

        Assert.True(isValid);
        Assert.Null(error);
        Assert.NotNull(notification);
        Assert.Equal(order, notification.Order);
        Assert.Equal(3500, notification.Amount.Cents);
        Assert.True(notification.IsSuccess);
        Assert.True(notification.IsBizum);
    }

    [Fact]
    public async Task DevMock_WhenInProduction_PassesThroughWithoutIntercepting()
    {
        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
            IsProduction = true, // Production enabled
            UseDevMock = true
        };

        bool nextCalled = false;
        var middleware = new RedsysDevMockMiddleware(
            next: ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            options: Options.Create(options),
            cryptoService: new RedsysCryptoService(),
            logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<RedsysDevMockMiddleware>.Instance);

        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Path = options.DevMockPath;
        context.Request.Method = "GET";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task DevMock_DispatchWebhook_RejectsExternalSsrfUrl()
    {
        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
            IsProduction = false,
            UseDevMock = true
        };

        var middleware = new RedsysDevMockMiddleware(
            next: ctx => Task.CompletedTask,
            options: Options.Create(options),
            cryptoService: new RedsysCryptoService(),
            logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<RedsysDevMockMiddleware>.Instance);

        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Path = $"{options.DevMockPath.TrimEnd('/')}/dispatch-webhook";
        context.Request.Method = "POST";
        context.Request.Host = new Microsoft.AspNetCore.Http.HostString("localhost", 5000);
        context.Request.Scheme = "http";

        // Malicious simulation request targeting internal AWS cloud metadata
        string maliciousBody = """
        {
            "order": "2026102201",
            "amount": "1000",
            "merchantUrl": "http://169.254.169.254/latest/meta-data/",
            "responseCode": "0000"
        }
        """;
        var bodyBytes = Encoding.UTF8.GetBytes(maliciousBody);
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        string responseJson = new StreamReader(context.Response.Body).ReadToEnd();
        Assert.Contains("SSRF Protection", responseJson);
    }

    [Fact]
    public async Task DevMock_RenderMockPaymentUi_EncodesPotentialXssPayload()
    {
        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
            IsProduction = false,
            UseDevMock = true
        };

        var middleware = new RedsysDevMockMiddleware(
            next: ctx => Task.CompletedTask,
            options: Options.Create(options),
            cryptoService: new RedsysCryptoService(),
            logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<RedsysDevMockMiddleware>.Instance);

        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.Path = options.DevMockPath;
        context.Request.Method = "POST";
        context.Request.ContentType = "application/x-www-form-urlencoded";

        string xssDescription = "<script>alert('xss')</script>";
        var dto = new RedsysMerchantParametersDto
        {
            Order = "2026102202",
            Amount = "5000",
            ProductDescription = xssDescription
        };

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(dto, RedsysJsonContext.Default.RedsysMerchantParametersDto);
        string base64Params = Convert.ToBase64String(jsonBytes);

        context.Request.Form = new Microsoft.AspNetCore.Http.FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                [RedsysPaymentResponseData.FormFieldMerchantParameters] = base64Params
            });

        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(200, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        string html = new StreamReader(context.Response.Body).ReadToEnd();

        // Must NOT contain unescaped script tag in HTML body
        Assert.DoesNotContain("<script>alert('xss')</script>", html);
        // Must contain HTML-encoded version
        Assert.Contains("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", html);
    }
}
