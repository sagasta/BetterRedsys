using System.Diagnostics;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.Diagnostics;
using BetterRedsys.Models;
using BetterRedsys.Services;
using BetterRedsys.Simulator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Tests;

public class RedsysConfigurationAndTelemetryTests
{
    private const string SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U";

    [Fact]
    public void RedsysOptions_FluentMethods_ConfigureExpectedProperties()
    {
        var options = new RedsysOptions();

        options.UseProduction("999008881", "1", SecretKey)
               .WithEuros()
               .WithDefaultLanguage(RedsysLanguage.Catalan)
               .WithWebhooks("/webhook", "/ok", "/ko")
               .WithCustomGateway("https://proxy.example.com/pay", "https://proxy.example.com/rest");

        Assert.Equal("999008881", options.Fuc);
        Assert.Equal("1", options.Terminal);
        Assert.Equal(SecretKey, options.SecretKey);
        Assert.True(options.IsProduction);
        Assert.Equal("978", options.Currency);
        Assert.Equal(RedsysLanguage.Catalan, options.DefaultLanguage);
        Assert.Equal("/webhook", options.MerchantUrl);
        Assert.Equal("/ok", options.UrlOk);
        Assert.Equal("/ko", options.UrlKo);
        Assert.Equal("https://proxy.example.com/pay", options.CustomGatewayUrl);
        Assert.Equal("https://proxy.example.com/rest", options.CustomRestGatewayUrl);

        // Switch to test
        options.UseTest("999008882", "2", SecretKey)
               .WithCurrency("840");

        Assert.Equal("999008882", options.Fuc);
        Assert.Equal("2", options.Terminal);
        Assert.False(options.IsProduction);
        Assert.Equal("840", options.Currency);
    }

    [Fact]
    public void AddBetterRedsys_WithFluentBuilder_ConfiguresAndValidatesOnStart()
    {
        var services = new ServiceCollection();

        IBetterRedsysBuilder builder = services.AddBetterRedsys(options =>
        {
            options.UseTest("999008881", "1", SecretKey)
                   .WithEuros()
                   .WithWebhooks("/api/webhook");
        })
        .AddHealthCheck()
        .AddSimulator();

        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<RedsysOptions>>().Value;
        Assert.Equal("999008881", options.Fuc);
        Assert.False(options.IsProduction);

        // Verify health check registration
        var hcOptions = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        Assert.Contains(hcOptions.Registrations, r => r.Name == "redsys");

        // Verify simulator options registration
        var simOptions = sp.GetRequiredService<IOptions<RedsysSimulatorOptions>>().Value;
        Assert.NotNull(simOptions);
    }

    [Fact]
    public void AddBetterRedsys_WhenOptionsInvalid_FailsValidationOnStart()
    {
        var services = new ServiceCollection();

        services.AddBetterRedsys(options =>
        {
            // Missing Fuc and SecretKey
            options.Terminal = "1";
        });

        var sp = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() =>
        {
            _ = sp.GetRequiredService<IOptions<RedsysOptions>>().Value;
        });

        Assert.Contains("Fuc", ex.Message);
        Assert.Contains("SecretKey", ex.Message);
    }

    [Fact]
    public void AddBetterRedsys_BindsToConfigurationAutomatically()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Redsys:Fuc"] = "123456789",
            ["Redsys:Terminal"] = "002",
            ["Redsys:SecretKey"] = SecretKey,
            ["Redsys:Currency"] = "978",
            ["Redsys:IsProduction"] = "true"
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.AddBetterRedsys(config);

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<RedsysOptions>>().Value;

        Assert.Equal("123456789", options.Fuc);
        Assert.Equal("002", options.Terminal);
        Assert.Equal(SecretKey, options.SecretKey);
        Assert.True(options.IsProduction);
    }

    [Fact]
    public void Telemetry_CreatePayment_RecordsActivityAndIncrementsCounter()
    {
        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = SecretKey
        };
        var crypto = new RedsysCryptoService();
        var client = new RedsysClient(Options.Create(options), crypto);

        Activity? capturedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == RedsysTelemetry.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                if (a.OperationName == "Redsys.CreatePayment" && Equals(a.GetTagItem("redsys.order"), "2026102299"))
                {
                    capturedActivity = a;
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var data = client.CreatePayment("2026102299", 50.00m)
            .WithDescription("Telemetry order")
            .ToFormData();

        Assert.NotNull(data);
        Assert.NotNull(capturedActivity);
        Assert.Equal("Redsys.CreatePayment", capturedActivity.OperationName);
        Assert.Equal("2026102299", capturedActivity.GetTagItem("redsys.order"));
        Assert.Equal(50.00m, capturedActivity.GetTagItem("redsys.amount"));
        Assert.Equal("999008881", capturedActivity.GetTagItem("redsys.fuc"));
    }

    [Fact]
    public void Telemetry_VerifyWebhook_RecordsActivityWithTags()
    {
        var options = new RedsysOptions
        {
            Fuc = "999008881",
            Terminal = "1",
            SecretKey = SecretKey
        };
        var crypto = new RedsysCryptoService();
        var client = new RedsysClient(Options.Create(options), crypto);

        string order = "2026102288";
        var simulatedDto = new RedsysNotificationParametersDto
        {
            Amount = "2500",
            Currency = "978",
            Order = order,
            MerchantCode = options.Fuc,
            Terminal = options.Terminal,
            Response = "0000",
            TransactionType = "0",
            SecurePayment = "1"
        };

        byte[] jsonBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(simulatedDto, RedsysJsonContext.Default.RedsysNotificationParametersDto);
        string paramsBase64 = Convert.ToBase64String(jsonBytes);
        byte[] diversifiedKey = crypto.DiversifyKey(options.SecretKey, order);
        string signature = crypto.ComputeSignature(paramsBase64, diversifiedKey);

        Activity? capturedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == RedsysTelemetry.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                if (a.OperationName == "Redsys.VerifyWebhook" && Equals(a.GetTagItem("redsys.order"), order))
                {
                    capturedActivity = a;
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        bool isValid = client.TryDecodeNotification(paramsBase64, signature, out var notification, out var error);

        Assert.True(isValid);
        Assert.NotNull(notification);
        Assert.NotNull(capturedActivity);
        Assert.Equal("Redsys.VerifyWebhook", capturedActivity.OperationName);
        Assert.Equal(order, capturedActivity.GetTagItem("redsys.order"));
        Assert.Equal("0000", capturedActivity.GetTagItem("redsys.response_code"));
        Assert.Equal(true, capturedActivity.GetTagItem("redsys.is_success"));
    }
}
