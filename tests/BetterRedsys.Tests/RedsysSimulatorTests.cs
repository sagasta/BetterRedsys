using System.Text;
using System.Text.Json;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.Models;
using BetterRedsys.Services;
using BetterRedsys.Simulator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Tests;

public class RedsysSimulatorTests
{
    [Fact]
    public void GetPaymentGatewayUrl_UsesCustomGatewayUrl_WhenSpecified()
    {
        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
            CustomGatewayUrl = "/redsys-simulator/pay"
        };

        Assert.Equal("/redsys-simulator/pay", options.GetPaymentGatewayUrl());
    }

    [Fact]
    public void AddBetterRedsysSimulator_ConfiguresCustomGatewayUrls()
    {
        var services = new ServiceCollection();
        services.AddBetterRedsys(options =>
        {
            options.Fuc = "999008881";
            options.Terminal = "1";
            options.SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U";
        });
        services.AddBetterRedsysSimulator();

        var sp = services.BuildServiceProvider();
        var redsysOpts = sp.GetRequiredService<IOptions<RedsysOptions>>().Value;

        Assert.Equal(RedsysSimulatorEndpoints.DefaultPath, redsysOpts.CustomGatewayUrl);
        Assert.Equal(RedsysSimulatorEndpoints.DefaultRestPath, redsysOpts.CustomRestGatewayUrl);
        Assert.Equal(RedsysSimulatorEndpoints.DefaultPath, redsysOpts.GetPaymentGatewayUrl());
        Assert.Equal(RedsysSimulatorEndpoints.DefaultRestPath, redsysOpts.GetRestGatewayUrl());
    }

    [Fact]
    public void Simulator_SimulationParameters_AreVerifiableByClient()
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

        var order = "2026102201";
        var simulatedDto = new SimulatorRestNotificationDto
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

        // Simular lo que hace el simulador:
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(simulatedDto, SimulatorRestJsonContext.Default.SimulatorRestNotificationDto);
        var paramsBase64 = Convert.ToBase64String(jsonBytes);

        var diversifiedKey = crypto.DiversifyKey(options.SecretKey, order);
        var signature = crypto.ComputeSignature(paramsBase64, diversifiedKey);

        // Validar que RedsysClient decodifica y verifica exitosamente
        var isValid = client.TryDecodeNotification(paramsBase64, signature, out var notification, out var error);

        Assert.True(isValid);
        Assert.Null(error);
        Assert.NotNull(notification);
        Assert.Equal(order, notification.Order);
        Assert.Equal(3500, notification.Amount.Cents);
        Assert.True(notification.IsSuccess);
        Assert.True(notification.IsBizum);
    }

    [Fact]
    public async Task Simulator_WhenInProduction_PassesThroughWithoutIntercepting()
    {
        var redsysOptions = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
            IsProduction = true // Production enabled
        };
        var simulatorOptions = new RedsysSimulatorOptions();

        var nextCalled = false;
        var middleware = new RedsysSimulatorMiddleware(
            next: ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            redsysOptions: Options.Create(redsysOptions),
            simulatorOptions: Options.Create(simulatorOptions),
            cryptoService: new RedsysCryptoService(),
            logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<RedsysSimulatorMiddleware>.Instance);

        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = simulatorOptions.Path,
                Method = "GET"
            }
        };

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Simulator_DispatchWebhook_RejectsExternalSsrfUrl()
    {
        var redsysOptions = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
            IsProduction = false
        };
        var simulatorOptions = new RedsysSimulatorOptions();

        var middleware = new RedsysSimulatorMiddleware(
            next: ctx => Task.CompletedTask,
            redsysOptions: Options.Create(redsysOptions),
            simulatorOptions: Options.Create(simulatorOptions),
            cryptoService: new RedsysCryptoService(),
            logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<RedsysSimulatorMiddleware>.Instance);

        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = $"{simulatorOptions.Path.TrimEnd('/')}/dispatch-webhook",
                Method = "POST",
                Host = new HostString("localhost", 5000),
                Scheme = "http"
            }
        };

        // Malicious simulation request targeting internal AWS cloud metadata
        const string maliciousBody = """
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
        var responseJson = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("SSRF Protection", responseJson);
    }

    [Fact]
    public async Task Simulator_RenderSimulatorPaymentUi_EncodesPotentialXssPayload()
    {
        var redsysOptions = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
            IsProduction = false
        };
        var simulatorOptions = new RedsysSimulatorOptions();

        var middleware = new RedsysSimulatorMiddleware(
            next: ctx => Task.CompletedTask,
            redsysOptions: Options.Create(redsysOptions),
            simulatorOptions: Options.Create(simulatorOptions),
            cryptoService: new RedsysCryptoService(),
            logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<RedsysSimulatorMiddleware>.Instance);

        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = simulatorOptions.Path,
                Method = "POST",
                ContentType = "application/x-www-form-urlencoded"
            }
        };

        var xssDescription = "<script>alert('xss')</script>";
        var dto = new SimulatorRestMerchantDto
        {
            Order = "2026102202",
            Amount = "5000",
            ProductDescription = xssDescription
        };

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(dto, SimulatorRestJsonContext.Default.SimulatorRestMerchantDto);
        var base64Params = Convert.ToBase64String(jsonBytes);

        context.Request.Form = new FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                [RedsysPaymentResponseData.FormFieldMerchantParameters] = base64Params
            });

        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(200, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var html = await new StreamReader(context.Response.Body).ReadToEndAsync();

        // Must NOT contain unescaped script tag in HTML body
        Assert.DoesNotContain("<script>alert('xss')</script>", html);
        // Must contain HTML-encoded version
        Assert.Contains("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", html);
    }

    [Fact]
    public void AddBetterRedsysSimulator_DoesNotOverrideGatewayUrls_InProduction()
    {
        var services = new ServiceCollection();
        services.AddBetterRedsys(options =>
        {
            options.UseProduction("999008881", "1", "sq7HjrUOBfKmC576ILgskD5srURiUz6U");
        });
        services.AddBetterRedsysSimulator();

        var sp = services.BuildServiceProvider();
        var redsysOpts = sp.GetRequiredService<IOptions<RedsysOptions>>().Value;

        Assert.Null(redsysOpts.CustomGatewayUrl);
        Assert.Null(redsysOpts.CustomRestGatewayUrl);
        Assert.Equal(RedsysEndpoints.Production, redsysOpts.GetPaymentGatewayUrl());
        Assert.Equal(RedsysEndpoints.RestProduction, redsysOpts.GetRestGatewayUrl());
    }

    [Theory]
    [InlineData("Staging", false, true)]
    [InlineData("Production", false, true)]
    [InlineData("Staging", true, false)]
    [InlineData("Development", false, false)]
    public async Task Simulator_OnlyInterceptsInDevelopmentUnlessExplicitlyAllowed(
        string environmentName,
        bool allowNonDevelopment,
        bool expectPassThrough)
    {
        var redsysOptions = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
            IsProduction = false
        };
        var simulatorOptions = new RedsysSimulatorOptions { AllowNonDevelopmentEnvironments = allowNonDevelopment };

        var nextCalled = false;
        var middleware = new RedsysSimulatorMiddleware(
            next: ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            redsysOptions: Options.Create(redsysOptions),
            simulatorOptions: Options.Create(simulatorOptions),
            cryptoService: new RedsysCryptoService(),
            logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<RedsysSimulatorMiddleware>.Instance,
            hostEnvironment: new TestHostEnvironment(environmentName));

        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = $"{simulatorOptions.Path.TrimEnd('/')}/dispatch-webhook",
                Method = "POST",
                Host = new HostString("localhost", 5000),
                Scheme = "http",
                Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"))
            },
            Response = { Body = new MemoryStream() }
        };

        await middleware.InvokeAsync(context);

        Assert.Equal(expectPassThrough, nextCalled);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "BetterRedsys.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
