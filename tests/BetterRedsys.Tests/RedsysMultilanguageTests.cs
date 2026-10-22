using System.Globalization;
using BetterRedsys.Configuration;
using BetterRedsys.Cryptography;
using BetterRedsys.Models;
using BetterRedsys.Services;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Tests;

public class RedsysMultilanguageTests
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
    [InlineData(RedsysLanguage.Spanish, "001")]
    [InlineData(RedsysLanguage.English, "002")]
    [InlineData(RedsysLanguage.Catalan, "003")]
    [InlineData(RedsysLanguage.French, "004")]
    [InlineData(RedsysLanguage.German, "005")]
    [InlineData(RedsysLanguage.Dutch, "006")]
    [InlineData(RedsysLanguage.Italian, "007")]
    [InlineData(RedsysLanguage.Swedish, "008")]
    [InlineData(RedsysLanguage.Portuguese, "009")]
    [InlineData(RedsysLanguage.Valencian, "010")]
    [InlineData(RedsysLanguage.Polish, "011")]
    [InlineData(RedsysLanguage.Galician, "012")]
    [InlineData(RedsysLanguage.Basque, "013")]
    public void RedsysLanguage_ToRedsysCode_Returns3DigitCode(RedsysLanguage language, string expectedCode)
    {
        Assert.Equal(expectedCode, language.ToRedsysCode());
    }

    [Theory]
    [InlineData(RedsysLanguage.Spanish, "es")]
    [InlineData(RedsysLanguage.English, "en")]
    [InlineData(RedsysLanguage.Catalan, "ca")]
    [InlineData(RedsysLanguage.French, "fr")]
    [InlineData(RedsysLanguage.German, "de")]
    [InlineData(RedsysLanguage.Dutch, "nl")]
    [InlineData(RedsysLanguage.Italian, "it")]
    [InlineData(RedsysLanguage.Swedish, "sv")]
    [InlineData(RedsysLanguage.Portuguese, "pt")]
    [InlineData(RedsysLanguage.Valencian, "ca-valencia")]
    [InlineData(RedsysLanguage.Polish, "pl")]
    [InlineData(RedsysLanguage.Galician, "gl")]
    [InlineData(RedsysLanguage.Basque, "eu")]
    public void RedsysLanguage_GetIsoCode_ReturnsCorrectHtmlLang(RedsysLanguage language, string expectedIso)
    {
        Assert.Equal(expectedIso, language.GetIsoCode());
    }

    [Theory]
    [InlineData("001", RedsysLanguage.Spanish)]
    [InlineData("002", RedsysLanguage.English)]
    [InlineData("003", RedsysLanguage.Catalan)]
    [InlineData("999", RedsysLanguage.Spanish)] // Fallback to default
    [InlineData("", RedsysLanguage.Spanish)]
    [InlineData(null, RedsysLanguage.Spanish)]
    public void RedsysLanguage_FromRedsysCode_ParsesCorrectly(string? code, RedsysLanguage expectedLanguage)
    {
        Assert.Equal(expectedLanguage, RedsysLanguageExtensions.FromRedsysCode(code));
    }

    [Theory]
    [InlineData("en-US", RedsysLanguage.English)]
    [InlineData("es-ES", RedsysLanguage.Spanish)]
    [InlineData("fr-FR", RedsysLanguage.French)]
    [InlineData("de-DE", RedsysLanguage.German)]
    [InlineData("it-IT", RedsysLanguage.Italian)]
    [InlineData("ca-ES", RedsysLanguage.Catalan)]
    public void RedsysLanguage_FromCultureInfo_MapsAccurately(string cultureName, RedsysLanguage expectedLanguage)
    {
        var culture = new CultureInfo(cultureName);
        Assert.Equal(expectedLanguage, RedsysLanguageExtensions.FromCultureInfo(culture));
    }

    [Theory]
    [InlineData(RedsysLanguage.Spanish)]
    [InlineData(RedsysLanguage.English)]
    [InlineData(RedsysLanguage.Catalan)]
    [InlineData(RedsysLanguage.French)]
    [InlineData(RedsysLanguage.German)]
    [InlineData(RedsysLanguage.Dutch)]
    [InlineData(RedsysLanguage.Italian)]
    [InlineData(RedsysLanguage.Swedish)]
    [InlineData(RedsysLanguage.Portuguese)]
    [InlineData(RedsysLanguage.Valencian)]
    [InlineData(RedsysLanguage.Polish)]
    [InlineData(RedsysLanguage.Galician)]
    [InlineData(RedsysLanguage.Basque)]
    public void RedsysResponseCatalog_All13Languages_ProvideAccurateDescriptions(RedsysLanguage language)
    {
        var successDesc = RedsysResponseCatalog.GetDescription("0000", language);
        Assert.False(string.IsNullOrWhiteSpace(successDesc));

        var error3Ds = RedsysResponseCatalog.GetDescription("0184", language);
        Assert.False(string.IsNullOrWhiteSpace(error3Ds));

        var timeout = RedsysResponseCatalog.GetDescription("0912", language);
        Assert.False(string.IsNullOrWhiteSpace(timeout));

        var rangeDesc = RedsysResponseCatalog.GetDescription("0155", language);
        Assert.False(string.IsNullOrWhiteSpace(rangeDesc));
    }

    [Theory]
    [InlineData(RedsysLanguage.Spanish)]
    [InlineData(RedsysLanguage.English)]
    [InlineData(RedsysLanguage.Catalan)]
    [InlineData(RedsysLanguage.French)]
    [InlineData(RedsysLanguage.German)]
    [InlineData(RedsysLanguage.Dutch)]
    [InlineData(RedsysLanguage.Italian)]
    [InlineData(RedsysLanguage.Swedish)]
    [InlineData(RedsysLanguage.Portuguese)]
    [InlineData(RedsysLanguage.Valencian)]
    [InlineData(RedsysLanguage.Polish)]
    [InlineData(RedsysLanguage.Galician)]
    [InlineData(RedsysLanguage.Basque)]
    public void RedsysBusinessTexts_All13Languages_ProvideCompleteHtmlTexts(RedsysLanguage language)
    {
        var texts = RedsysBusinessTexts.GetHtmlTexts(language);

        Assert.NotNull(texts);
        Assert.False(string.IsNullOrWhiteSpace(texts.PageTitle));
        Assert.False(string.IsNullOrWhiteSpace(texts.Heading));
        Assert.False(string.IsNullOrWhiteSpace(texts.Subheading));
        Assert.False(string.IsNullOrWhiteSpace(texts.OrderLabel));
        Assert.False(string.IsNullOrWhiteSpace(texts.AmountLabel));
        Assert.False(string.IsNullOrWhiteSpace(texts.DefaultButtonText));
    }

    [Theory]
    [InlineData(RedsysLanguage.French, "fr", "Connexion à Redsys", "Commande :")]
    [InlineData(RedsysLanguage.German, "de", "Verbindung zu Redsys", "Bestellung:")]
    [InlineData(RedsysLanguage.Italian, "it", "Connessione a Redsys", "Ordine:")]
    [InlineData(RedsysLanguage.Catalan, "ca", "Connectant amb Redsys", "Comanda:")]
    [InlineData(RedsysLanguage.Basque, "eu", "Redsysekin konektatzen", "Eskaera:")]
    [InlineData(RedsysLanguage.Galician, "gl", "Conectando con Redsys", "Pedido:")]
    [InlineData(RedsysLanguage.Portuguese, "pt", "A ligar à Redsys", "Encomenda:")]
    [InlineData(RedsysLanguage.Dutch, "nl", "Verbinden met Redsys", "Bestelling:")]
    [InlineData(RedsysLanguage.Swedish, "sv", "Ansluter till Redsys", "Beställning:")]
    [InlineData(RedsysLanguage.Polish, "pl", "Łączenie z Redsys", "Zamówienie:")]
    public void ToAutoSubmitHtml_GeneratesCorrectHtml_ForDifferentSupportedLanguages(
        RedsysLanguage language,
        string expectedLangAttr,
        string expectedHeading,
        string expectedOrderLabel)
    {
        var client = CreateClient();
        var html = client.CreatePayment("2026102201", 39.90m)
            .WithConsumerLanguage(language)
            .ToAutoSubmitHtml(language);

        Assert.Contains($"<html lang=\"{expectedLangAttr}\">", html);
        Assert.Contains(expectedHeading, html);
        Assert.Contains(expectedOrderLabel, html);
    }

    [Fact]
    public void Notification_ProvidesBilingualAndMultilingualAccessors()
    {
        var notification = new RedsysNotification
        {
            ResponseCode = "0184",
            Order = "0001"
        };

        Assert.Contains("3D Secure", notification.ResponseDescription);
        Assert.Contains("Cardholder authentication error", notification.ResponseDescriptionEn);
        Assert.Contains("titulaire", notification.GetResponseDescription(RedsysLanguage.French));
        Assert.Contains("Karteninhabers", notification.GetResponseDescription(RedsysLanguage.German));
        Assert.Contains("titularraren", notification.GetResponseDescription(RedsysLanguage.Basque));
    }
}
