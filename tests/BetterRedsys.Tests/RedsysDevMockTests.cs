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
}
