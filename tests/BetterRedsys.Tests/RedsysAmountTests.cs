using BetterRedsys.Models;

namespace BetterRedsys.Tests;

public class RedsysAmountTests
{
    [Fact]
    public void FromEuros_Decimal_CalculatesExactCents()
    {
        var amount = RedsysAmount.FromEuros(15.50m);
        Assert.Equal(1550, amount.Cents);
        Assert.Equal(15.50m, amount.Euros);
        Assert.Equal("1550", amount.ToRedsysString());
    }

    [Fact]
    public void FromEuros_SmallAmounts_HandlesExactPennies()
    {
        var amount = RedsysAmount.FromEuros(0.01m);
        Assert.Equal(1, amount.Cents);
        Assert.Equal(0.01m, amount.Euros);
        Assert.Equal("1", amount.ToRedsysString());
    }

    [Fact]
    public void FromEuros_Int_MultipliesByHundred()
    {
        var amount = RedsysAmount.FromEuros(25);
        Assert.Equal(2500, amount.Cents);
        Assert.Equal(25.00m, amount.Euros);
        Assert.Equal("2500", amount.ToRedsysString());
    }

    [Fact]
    public void ImplicitConversions_WorkSeamlessly()
    {
        RedsysAmount fromDecimal = 49.99m;
        RedsysAmount fromInt = 100;

        Assert.Equal(4999, fromDecimal.Cents);
        Assert.Equal(10000, fromInt.Cents);

        decimal backToDecimal = fromDecimal;
        long backToLong = fromInt;

        Assert.Equal(49.99m, backToDecimal);
        Assert.Equal(10000L, backToLong);
    }

    [Fact]
    public void ArithmeticAndComparisonOperators_WorkCorrectly()
    {
        RedsysAmount a = 10.00m;
        RedsysAmount b = 5.50m;

        RedsysAmount sum = a + b;
        RedsysAmount diff = a - b;

        Assert.Equal(1550, sum.Cents);
        Assert.Equal(450, diff.Cents);
        Assert.True(a > b);
        Assert.True(b < a);
        Assert.True(a >= b);
        Assert.True(b <= a);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RedsysAmount.FromEuros(-1.00m));
        Assert.Throws<ArgumentOutOfRangeException>(() => RedsysAmount.FromCents(-100));
        Assert.Throws<ArgumentOutOfRangeException>(() => (RedsysAmount)(-5));
    }
}
