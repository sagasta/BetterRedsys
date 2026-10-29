using System.Text;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.Models;
using BetterRedsys.Services;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Tests;

public class RedsysNotificationTests
{
    private readonly RedsysOptions _options = new()
    {
        Fuc = "999008881",
        Terminal = "1",
        SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
        Currency = "978"
    };

    private readonly RedsysCryptoService _crypto = new();

    private RedsysClient CreateClient() => new(Options.Create(_options), _crypto);

    [Theory]
    [InlineData("0000", true)]
    [InlineData("0050", true)]
    [InlineData("0099", true)]
    [InlineData("0900", true)]
    [InlineData("0400", true)]
    [InlineData("0101", false)]
    [InlineData("0184", false)]
    [InlineData("0912", false)]
    [InlineData("9915", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSuccess_EvaluatesCorrectly(string? code, bool expectedSuccess)
    {
        Assert.Equal(expectedSuccess, RedsysResponseCatalog.IsSuccess(code));
    }

    [Theory]
    [InlineData("0101", "Tarjeta caducada.")]
    [InlineData("0116", "Disponible o saldo insuficiente.")]
    [InlineData("0180", "Tarjeta ajena al servicio o denegada por entidad.")]
    [InlineData("0184", "Error en la autenticación del titular (fallo en 3D Secure / SCA / SMS OTP).")]
    [InlineData("0190", "Denegación del emisor sin especificar motivo.")]
    [InlineData("0912", "Centro emisor no disponible (entidad bancaria del cliente inaccesible).")]
    [InlineData("9915", "Transacción cancelada voluntariamente por el usuario.")]
    public void GetDescription_ReturnsAccurateMessages(string code, string expectedSubstring)
    {
        var desc = RedsysResponseCatalog.GetDescription(code);
        Assert.Contains(expectedSubstring, desc);
    }

    [Fact]
    public void DecodeNotification_ValidPayload_ParsesAllFieldsAndVerifiesSignature()
    {
        var order = "0001";
        var rawParamsJson = """
                            {
                                "Ds_Date": "22%2F10%2F2026",
                                "Ds_Hour": "15%3A30",
                                "Ds_Amount": "4500",
                                "Ds_Currency": "978",
                                "Ds_Order": "0001",
                                "Ds_MerchantCode": "999008881",
                                "Ds_Terminal": "001",
                                "Ds_Response": "0000",
                                "Ds_AuthorisationCode": "123456",
                                "Ds_TransactionType": "0",
                                "Ds_SecurePayment": "1",
                                "Ds_Card_Country": "724",
                                "Ds_Card_Type": "C",
                                "Ds_Card_Brand": "1",
                                "Ds_ProcessedPayMethod": "z"
                            }
                            """;

        var paramsBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawParamsJson));
        var diversifiedKey = _crypto.DiversifyKey(_options.SecretKey, order);
        var validSignature = _crypto.ComputeSignature(paramsBase64, diversifiedKey);

        var client = CreateClient();
        var notification = client.DecodeNotification(paramsBase64, validSignature);

        Assert.NotNull(notification);
        Assert.True(notification.IsSuccess);
        Assert.True(notification.IsBizum);
        Assert.Equal("0001", notification.Order);
        Assert.Equal(4500, notification.Amount.Cents);
        Assert.Equal(45.00m, notification.Amount.Euros);
        Assert.Equal("0000", notification.ResponseCode);
        Assert.Equal("123456", notification.AuthorisationCode);
        Assert.True(notification.SecurePayment);
        Assert.Equal("724", notification.CardCountry);
        Assert.Equal("z", notification.ProcessedPayMethod);
    }

    [Fact]
    public void DecodeNotification_TamperedSignature_ThrowsInvalidOperationException()
    {
        var rawParamsJson = """{"Ds_Order":"0002","Ds_Amount":"1000","Ds_Response":"0000"}""";
        var paramsBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawParamsJson));
        var fakeSignature = "dGFtcGVyZWRfc2lnbmF0dXJlX2Zvcl90ZXN0";

        var client = CreateClient();

        Assert.Throws<InvalidOperationException>(() =>
            client.DecodeNotification(paramsBase64, fakeSignature));
    }

    [Fact]
    public void DecodeNotification_WithCofToken_ParsesCardAndIdentifierProperties()
    {
        var order = "0003";
        var rawParamsJson = """
                            {
                                "Ds_Order": "0003",
                                "Ds_Amount": "1999",
                                "Ds_Response": "0000",
                                "Ds_Merchant_Identifier": "TOKEN9876543210ABCDEF",
                                "Ds_Card_Number": "454881******0004",
                                "Ds_ExpiryDate": "2812"
                            }
                            """;

        var paramsBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawParamsJson));
        var diversifiedKey = _crypto.DiversifyKey(_options.SecretKey, order);
        var validSignature = _crypto.ComputeSignature(paramsBase64, diversifiedKey);

        var client = CreateClient();
        var notification = client.DecodeNotification(paramsBase64, validSignature);

        Assert.NotNull(notification);
        Assert.Equal("TOKEN9876543210ABCDEF", notification.MerchantIdentifier);
        Assert.Equal("TOKEN9876543210ABCDEF", notification.Identifier);
        Assert.Equal("454881******0004", notification.CardNumber);
        Assert.Equal("2812", notification.ExpiryDate);
    }

    [Theory]
    [InlineData("15%2F07%2F2026", "10%3A30", 2)] // Summer time (CEST)
    [InlineData("15%2F01%2F2026", "10%3A30", 1)] // Winter time (CET)
    public void Create_InterpretsTransactionDateTimeAsMadridTime(string date, string hour, int expectedOffsetHours)
    {
        var dto = new RedsysNotificationParametersDto { Order = "2026102201", Date = date, Hour = hour };

        var notification = RedsysNotification.Create(dto, "params", "signature");

        Assert.NotNull(notification.TransactionDateTime);
        Assert.Equal(TimeSpan.FromHours(expectedOffsetHours), notification.TransactionDateTime.Value.Offset);
        Assert.Equal(10, notification.TransactionDateTime.Value.Hour);
        Assert.Equal(30, notification.TransactionDateTime.Value.Minute);
    }
}
