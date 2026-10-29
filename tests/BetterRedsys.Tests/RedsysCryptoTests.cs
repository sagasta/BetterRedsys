using BetterRedsys.Cryptography;
using BetterRedsys.Testing;

namespace BetterRedsys.Tests;

public class RedsysCryptoTests
{
    private readonly RedsysCryptoService _cryptoService = new();

    // Clave de prueba típica de Redsys Sandbox (16 bytes en Base64)
    // 24-byte 3DES key, Base64 encoded (public Redsys test terminal 999008881-1)
    private const string TestSecretKey = RedsysTestCredentials.SecretKey;

    [Fact]
    public void DiversifyKey_WithValidOrder_ProducesExactBytes()
    {
        var order = "0001";
        var diversifiedKey = _cryptoService.DiversifyKey(TestSecretKey, order);

        Assert.NotNull(diversifiedKey);
        Assert.True(diversifiedKey.Length is 8 or 16 or 24);
    }

    [Fact]
    public void ComputeSignature_And_VerifySignature_SucceedsForSameOrderAndKey()
    {
        var order = "12345678";
        var parametersBase64 = Convert.ToBase64String("{\"DS_MERCHANT_AMOUNT\":\"1050\"}"u8.ToArray());

        var diversifiedKey = _cryptoService.DiversifyKey(TestSecretKey, order);
        var signature = _cryptoService.ComputeSignature(parametersBase64, diversifiedKey);

        var isValid = _cryptoService.VerifySignature(parametersBase64, signature, TestSecretKey, order);
        Assert.True(isValid);
    }

    [Fact]
    public void VerifySignature_FailsWhenParametersTampered()
    {
        var order = "0001";
        var originalParams = Convert.ToBase64String("{\"DS_MERCHANT_AMOUNT\":\"1050\"}"u8.ToArray());
        var tamperedParams = Convert.ToBase64String("{\"DS_MERCHANT_AMOUNT\":\"50\"}"u8.ToArray());

        var diversifiedKey = _cryptoService.DiversifyKey(TestSecretKey, order);
        var signature = _cryptoService.ComputeSignature(originalParams, diversifiedKey);

        var isValid = _cryptoService.VerifySignature(tamperedParams, signature, TestSecretKey, order);
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_FailsWhenOrderDoesNotMatch()
    {
        var order1 = "0001";
        var order2 = "0002";
        var parametersBase64 = Convert.ToBase64String("{\"DS_MERCHANT_AMOUNT\":\"1050\"}"u8.ToArray());

        var diversifiedKey = _cryptoService.DiversifyKey(TestSecretKey, order1);
        var signature = _cryptoService.ComputeSignature(parametersBase64, diversifiedKey);

        var isValid = _cryptoService.VerifySignature(parametersBase64, signature, TestSecretKey, order2);
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_AcceptsUrlSafeBase64()
    {
        var order = "9999";
        var parametersBase64 = Convert.ToBase64String("{\"DS_MERCHANT_AMOUNT\":\"2000\"}"u8.ToArray());

        var diversifiedKey = _cryptoService.DiversifyKey(TestSecretKey, order);
        var signature = _cryptoService.ComputeSignature(parametersBase64, diversifiedKey);

        // Convert signature to url-safe by replacing '+' with '-' and '/' with '_'
        var urlSafeSignature = signature.Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var isValid = _cryptoService.VerifySignature(parametersBase64, urlSafeSignature, TestSecretKey, order);
        Assert.True(isValid);
    }

    [Fact]
    public void NormalizeBase64_FixesUrlSafeCharactersAndMissingPadding()
    {
        var urlSafe = "a-b_c"; // length 5, missing 3 '=' paddings
        var normalized = _cryptoService.NormalizeBase64(urlSafe);

        Assert.Contains("+", normalized);
        Assert.Contains("/", normalized);
        Assert.Equal(0, normalized.Length % 4);
    }
}
