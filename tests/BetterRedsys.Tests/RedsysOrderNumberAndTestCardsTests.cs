using BetterRedsys.Models;
using BetterRedsys.Testing;

namespace BetterRedsys.Tests;

public class RedsysOrderNumberAndTestCardsTests
{
    [Fact]
    public void Generate_ProducesValid12DigitOrderNumber()
    {
        var order = RedsysOrderNumber.Generate();

        Assert.Equal(12, order.Length);
        Assert.True(RedsysOrderNumber.IsValid(order, out var error), error);

        // First 4 characters must be numeric
        for (var i = 0; i < 4; i++)
        {
            Assert.True(char.IsAsciiDigit(order[i]));
        }

        // All characters must be alphanumeric
        Assert.True(order.All(char.IsAsciiLetterOrDigit));
    }

    [Theory]
    [InlineData("2026102201")]      // 10 digits, first 4 numeric
    [InlineData("1234")]            // Min length 4, all numeric
    [InlineData("123456789012")]    // Max length 12
    [InlineData("9999ABCDEF12")]    // Alphanumeric with first 4 digits
    public void IsValid_WithValidOrder_ReturnsTrue(string order)
    {
        var isValid = RedsysOrderNumber.IsValid(order, out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]             // Too short (< 4)
    [InlineData("1234567890123")]   // Too long (> 12)
    [InlineData("A234567890")]      // First character not a digit
    [InlineData("12A4567890")]      // Third character not a digit
    [InlineData("1234-5678")]       // Contains invalid hyphen
    [InlineData("1234 5678")]       // Contains space
    public void IsValid_WithInvalidOrder_ReturnsFalse(string? order)
    {
        var isValid = RedsysOrderNumber.IsValid(order, out var error);

        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_WithInvalidOrder_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => RedsysOrderNumber.Validate("INV"));
    }

    [Fact]
    public void TestCards_ExposeOfficialConstants()
    {
        Assert.Equal("4548810000000003", RedsysTestCards.Authorized);
        Assert.Equal("4548810000000116", RedsysTestCards.InsufficientFunds);
        Assert.Equal("4548810000000180", RedsysTestCards.Expired);
        Assert.Equal("4548810000000184", RedsysTestCards.Authentication3DsError);
        Assert.Equal("4548810000000190", RedsysTestCards.IssuerDeclined);
        Assert.Equal("1234", RedsysTestCards.SmsCode);
        Assert.Equal("123", RedsysTestCards.DefaultCvv);
        Assert.Equal("12/28", RedsysTestCards.DefaultExpiry);

        Assert.Equal("999008881", RedsysTestCredentials.Fuc);
        Assert.Equal("1", RedsysTestCredentials.Terminal);
        Assert.Equal("sq7HjrUOBfKmC576ILgskD5srURiUz6U", RedsysTestCredentials.SecretKey);
    }
}
