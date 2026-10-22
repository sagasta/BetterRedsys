using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.Models;
using BetterRedsys.Services;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Tests;

public class RedsysPaymentBuilderTests
{
    private readonly RedsysOptions _options = new()
    {
        Fuc = "999008881",
        Terminal = "1",
        SecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
        Currency = "978",
        MerchantUrl = "https://example.com/webhook",
        UrlOk = "https://example.com/ok",
        UrlKo = "https://example.com/ko",
        IsProduction = false
    };

    private readonly RedsysCryptoService _crypto = new();

    private RedsysClient CreateClient()
    {
        return new RedsysClient(Options.Create(_options), _crypto);
    }

    [Theory]
    [InlineData("612345678", "612345678")]
    [InlineData("+34 612 34 56 78", "612345678")]
    [InlineData("0034 712345678", "712345678")]
    [InlineData("+34-600-11-22-33", "600112233")]
    [InlineData("799887766", "799887766")]
    public void SanitizeSpanishMobilePhone_ValidNumbers_Returns9Digits(string input, string expected)
    {
        string sanitized = RedsysPaymentBuilder.SanitizeSpanishMobilePhone(input);
        Assert.Equal(expected, sanitized);
    }

    [Theory]
    [InlineData("912345678")] // Fijo español
    [InlineData("812345678")] // Fijo español
    [InlineData("12345")]     // Demasiado corto
    [InlineData("+1 555 123 4567")] // Teléfono de USA
    public void SanitizeSpanishMobilePhone_InvalidNumbers_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => RedsysPaymentBuilder.SanitizeSpanishMobilePhone(input));
    }

    [Theory]
    [InlineData("0001")]
    [InlineData("12345678")]
    [InlineData("1234abc")]
    [InlineData("202610220001")]
    public void ValidateOrder_ValidOrders_Passes(string order)
    {
        RedsysPaymentBuilder.ValidateOrder(order);
    }

    [Theory]
    [InlineData("001")]         // Menos de 4 chars
    [InlineData("1234567890123")] // Más de 12 chars
    [InlineData("abc1234")]      // Primeros 4 no son dígitos
    [InlineData("12a456")]       // Letra en los primeros 4 dígitos
    [InlineData("1234-567")]     // Carácter especial
    public void ValidateOrder_InvalidOrders_ThrowsArgumentException(string order)
    {
        Assert.Throws<ArgumentException>(() => RedsysPaymentBuilder.ValidateOrder(order));
    }

    [Fact]
    public void ToFormData_BuildsSignedResponseData()
    {
        var client = CreateClient();
        var data = client.CreatePayment("123456", 29.99m)
            .WithDescription("Suscripción Pro")
            .WithBizum()
            .WithCustomerPhone("+34 612 345 678")
            .ToFormData();

        Assert.NotNull(data);
        Assert.Equal("HMAC_SHA256_V1", data.SignatureVersion);
        Assert.False(string.IsNullOrWhiteSpace(data.MerchantParameters));
        Assert.False(string.IsNullOrWhiteSpace(data.Signature));
        Assert.Equal("123456", data.Order);
        Assert.Equal(2999, data.Amount.Cents);
        Assert.Equal(RedsysEndpoints.Test, data.ActionUrl);

        // Validar que la firma generada por el builder es criptográficamente válida
        bool isSigValid = _crypto.VerifySignature(data.MerchantParameters, data.Signature, _options.SecretKey, "123456");
        Assert.True(isSigValid);
    }

    [Fact]
    public void ToAutoSubmitHtml_GeneratesSelfSubmittingHtmlForm()
    {
        var client = CreateClient();
        string html = client.CreatePayment("99990001", 10.00m)
            .WithCardOrBizum()
            .ToAutoSubmitHtml();

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("id=\"redsys-form\"", html);
        Assert.Contains(RedsysPaymentResponseData.FormFieldSignatureVersion, html);
        Assert.Contains(RedsysPaymentResponseData.FormFieldMerchantParameters, html);
        Assert.Contains(RedsysPaymentResponseData.FormFieldSignature, html);
        Assert.Contains("form.submit()", html);
    }
}
