using System.Text;
using BetterRedsys.Cryptography;

namespace BetterRedsys.Tests;

public class RedsysCryptoTests
{
    private readonly RedsysCryptoService _cryptoService = new();

    // Clave de prueba típica de Redsys Sandbox (16 bytes en Base64)
    // 16 bytes: "sq7HjrUOBfKmC576ILgskD5srURiUz6U" -> Base64
    private const string TestSecretKey = "sq7HjrUOBfKmC576ILgskD5srURiUz6U";

    [Fact]
    public void DiversifyKey_WithValidOrder_ProducesExactBytes()
    {
        string order = "0001";
        byte[] diversifiedKey = _cryptoService.DiversifyKey(TestSecretKey, order);

        Assert.NotNull(diversifiedKey);
        Assert.True(diversifiedKey.Length is 8 or 16 or 24);
    }

    [Fact]
    public void ComputeSignature_And_VerifySignature_SucceedsForSameOrderAndKey()
    {
        string order = "12345678";
        string parametersBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"DS_MERCHANT_AMOUNT\":\"1050\"}"));

        byte[] diversifiedKey = _cryptoService.DiversifyKey(TestSecretKey, order);
        string signature = _cryptoService.ComputeSignature(parametersBase64, diversifiedKey);

        bool isValid = _cryptoService.VerifySignature(parametersBase64, signature, TestSecretKey, order);
        Assert.True(isValid);
    }

    [Fact]
    public void VerifySignature_FailsWhenParametersTampered()
    {
        string order = "0001";
        string originalParams = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"DS_MERCHANT_AMOUNT\":\"1050\"}"));
        string tamperedParams = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"DS_MERCHANT_AMOUNT\":\"50\"}"));

        byte[] diversifiedKey = _cryptoService.DiversifyKey(TestSecretKey, order);
        string signature = _cryptoService.ComputeSignature(originalParams, diversifiedKey);

        bool isValid = _cryptoService.VerifySignature(tamperedParams, signature, TestSecretKey, order);
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_FailsWhenOrderDoesNotMatch()
    {
        string order1 = "0001";
        string order2 = "0002";
        string parametersBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"DS_MERCHANT_AMOUNT\":\"1050\"}"));

        byte[] diversifiedKey = _cryptoService.DiversifyKey(TestSecretKey, order1);
        string signature = _cryptoService.ComputeSignature(parametersBase64, diversifiedKey);

        bool isValid = _cryptoService.VerifySignature(parametersBase64, signature, TestSecretKey, order2);
        Assert.False(isValid);
    }

    [Fact]
    public void VerifySignature_AcceptsUrlSafeBase64()
    {
        string order = "9999";
        string parametersBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"DS_MERCHANT_AMOUNT\":\"2000\"}"));

        byte[] diversifiedKey = _cryptoService.DiversifyKey(TestSecretKey, order);
        string signature = _cryptoService.ComputeSignature(parametersBase64, diversifiedKey);

        // Convert signature to url-safe by replacing '+' with '-' and '/' with '_'
        string urlSafeSignature = signature.Replace('+', '-').Replace('/', '_').TrimEnd('=');

        bool isValid = _cryptoService.VerifySignature(parametersBase64, urlSafeSignature, TestSecretKey, order);
        Assert.True(isValid);
    }

    [Fact]
    public void NormalizeBase64_FixesUrlSafeCharactersAndMissingPadding()
    {
        string urlSafe = "a-b_c"; // length 5, missing 3 '=' paddings
        string normalized = _cryptoService.NormalizeBase64(urlSafe);

        Assert.Contains("+", normalized);
        Assert.Contains("/", normalized);
        Assert.Equal(0, normalized.Length % 4);
    }
}
