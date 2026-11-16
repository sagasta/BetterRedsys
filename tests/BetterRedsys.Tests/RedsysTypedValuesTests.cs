using System.Text;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.Models;
using BetterRedsys.Services;
using BetterRedsys.Testing;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Tests;

public class RedsysTypedValuesTests
{
    [Theory]
    [InlineData(RedsysTransactionType.Authorization, "0")]
    [InlineData(RedsysTransactionType.Preauthorization, "1")]
    [InlineData(RedsysTransactionType.PreauthorizationConfirmation, "2")]
    [InlineData(RedsysTransactionType.Refund, "3")]
    [InlineData(RedsysTransactionType.PreauthorizationCancellation, "9")]
    public void TransactionType_RoundTripsRedsysCode(RedsysTransactionType type, string code)
    {
        Assert.Equal(code, type.ToRedsysCode());
        Assert.True(RedsysTransactionTypeExtensions.TryParseRedsysCode(code, out var parsed));
        Assert.Equal(type, parsed);
    }

    [Fact]
    public void TransactionType_UnknownCode_IsNotParsed()
    {
        Assert.False(RedsysTransactionTypeExtensions.TryParseRedsysCode("Z", out _));
        Assert.False(RedsysTransactionTypeExtensions.TryParseRedsysCode(null, out _));
    }

    [Theory]
    [InlineData(RedsysCurrency.EUR, "978")]
    [InlineData(RedsysCurrency.USD, "840")]
    [InlineData(RedsysCurrency.AUD, "036")]
    [InlineData(RedsysCurrency.ARS, "032")]
    public void Currency_IsFormattedAsThreeDigitIsoCode(RedsysCurrency currency, string code)
    {
        Assert.Equal(code, currency.ToIsoCode());
        Assert.True(RedsysCurrencyExtensions.TryParseIsoCode(code, out var parsed));
        Assert.Equal(currency, parsed);
    }

    [Fact]
    public void Currency_UnknownCode_IsNotParsed()
    {
        Assert.False(RedsysCurrencyExtensions.TryParseIsoCode("999", out _));
        Assert.False(RedsysCurrencyExtensions.TryParseIsoCode("abc", out _));
    }

    [Fact]
    public void Builder_TypedOverloads_WriteRedsysCodes()
    {
        var options = new RedsysOptions
        {
            Fuc = RedsysTestCredentials.Fuc,
            Terminal = RedsysTestCredentials.Terminal,
            SecretKey = RedsysTestCredentials.SecretKey
        };
        var client = new RedsysClient(Options.Create(options), new RedsysCryptoService());

        var formData = client.CreatePayment("0001ABC", 10m)
            .WithTransactionType(RedsysTransactionType.Preauthorization)
            .WithCurrency(RedsysCurrency.GBP)
            .ToFormData();

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(formData.MerchantParameters));
        Assert.Contains("\"DS_MERCHANT_TRANSACTIONTYPE\":\"1\"", json);
        Assert.Contains("\"DS_MERCHANT_CURRENCY\":\"826\"", json);
    }

    [Fact]
    public void Options_WithTypedCurrency_SetsIsoCode()
    {
        var options = new RedsysOptions().WithCurrency(RedsysCurrency.CHF);
        Assert.Equal("756", options.Currency);
    }
}
