using System.Collections.Frozen;

namespace BetterRedsys.Models;

/// <summary>
/// Container record holding localized text elements for the self-submitting HTML redirect page.
/// </summary>
/// <param name="PageTitle">HTML title tag content.</param>
/// <param name="Heading">Main heading content (h1).</param>
/// <param name="Subheading">Informational description text.</param>
/// <param name="OrderLabel">Label for the order field.</param>
/// <param name="AmountLabel">Label for the amount field.</param>
/// <param name="DefaultButtonText">Default fallback button text when JavaScript is disabled.</param>
public sealed record RedsysHtmlTexts(
    string PageTitle,
    string Heading,
    string Subheading,
    string OrderLabel,
    string AmountLabel,
    string DefaultButtonText);

/// <summary>
/// Centralized provider of localized business texts for all 13 languages supported by Redsys SIS.
/// </summary>
public static class RedsysBusinessTexts
{
    private static readonly FrozenDictionary<RedsysLanguage, RedsysHtmlTexts> HtmlTexts = new Dictionary<RedsysLanguage, RedsysHtmlTexts>
    {
        [RedsysLanguage.Spanish] = new(
            PageTitle: "Conectando con la pasarela de pago...",
            Heading: "Conectando con Redsys",
            Subheading: "Por favor espera un instante mientras te transferimos de forma segura al portal bancario.",
            OrderLabel: "Pedido:",
            AmountLabel: "Importe:",
            DefaultButtonText: "Continuar a la pasarela de pago..."),

        [RedsysLanguage.English] = new(
            PageTitle: "Connecting to payment gateway...",
            Heading: "Connecting to Redsys",
            Subheading: "Please wait a moment while we securely transfer you to the bank portal.",
            OrderLabel: "Order:",
            AmountLabel: "Amount:",
            DefaultButtonText: "Continue to payment gateway..."),

        [RedsysLanguage.Catalan] = new(
            PageTitle: "Connectant amb la passarel·la de pagament...",
            Heading: "Connectant amb Redsys",
            Subheading: "Si us plau, espereu un instant mentre us transferim de manera segura al portal bancari.",
            OrderLabel: "Comanda:",
            AmountLabel: "Import:",
            DefaultButtonText: "Continuar a la passarel·la de pagament..."),

        [RedsysLanguage.French] = new(
            PageTitle: "Connexion à la passerelle de paiement...",
            Heading: "Connexion à Redsys",
            Subheading: "Veuillez patienter un instant pendant que nous vous redirigeons en toute sécurité vers le portail bancaire.",
            OrderLabel: "Commande :",
            AmountLabel: "Montant :",
            DefaultButtonText: "Continuer vers la passerelle de paiement..."),

        [RedsysLanguage.German] = new(
            PageTitle: "Verbindung mit dem Zahlungs-Gateway...",
            Heading: "Verbindung zu Redsys",
            Subheading: "Bitte warten Sie einen Moment, während Sie sicher zum Bankportal weitergeleitet werden.",
            OrderLabel: "Bestellung:",
            AmountLabel: "Betrag:",
            DefaultButtonText: "Weiter zum Zahlungs-Gateway..."),

        [RedsysLanguage.Dutch] = new(
            PageTitle: "Verbinden met de betalingsgateway...",
            Heading: "Verbinden met Redsys",
            Subheading: "Een ogenblik geduld terwijl wij u veilig doorsturen naar het bankportaal.",
            OrderLabel: "Bestelling:",
            AmountLabel: "Bedrag:",
            DefaultButtonText: "Doorgaan naar de betalingsgateway..."),

        [RedsysLanguage.Italian] = new(
            PageTitle: "Connessione al gateway di pagamento...",
            Heading: "Connessione a Redsys",
            Subheading: "Attendere un momento mentre vieni trasferito in modo sicuro al portale bancario.",
            OrderLabel: "Ordine:",
            AmountLabel: "Importo:",
            DefaultButtonText: "Continua al gateway di pagamento..."),

        [RedsysLanguage.Swedish] = new(
            PageTitle: "Ansluter till betalningsgatewayen...",
            Heading: "Ansluter till Redsys",
            Subheading: "Vänta ett ögonblick medan vi säkert vidarebefordrar dig till bankportalen.",
            OrderLabel: "Beställning:",
            AmountLabel: "Belopp:",
            DefaultButtonText: "Fortsätt till betalningsgatewayen..."),

        [RedsysLanguage.Portuguese] = new(
            PageTitle: "A ligar ao gateway de pagamento...",
            Heading: "A ligar à Redsys",
            Subheading: "Aguarde um momento enquanto o transferimos com segurança para o portal bancário.",
            OrderLabel: "Encomenda:",
            AmountLabel: "Montante:",
            DefaultButtonText: "Continuar para o gateway de pagamento..."),

        [RedsysLanguage.Valencian] = new(
            PageTitle: "Connectant amb la passarel·la de pagament...",
            Heading: "Connectant amb Redsys",
            Subheading: "Per favor, espereu un instant mentre vos transferim de manera segura al portal bancari.",
            OrderLabel: "Comanda:",
            AmountLabel: "Import:",
            DefaultButtonText: "Continuar a la passarel·la de pagament..."),

        [RedsysLanguage.Polish] = new(
            PageTitle: "Łączenie z bramką płatniczą...",
            Heading: "Łączenie z Redsys",
            Subheading: "Proszę czekać, trwa bezpieczne przekierowanie do portalu bankowego.",
            OrderLabel: "Zamówienie:",
            AmountLabel: "Kwota:",
            DefaultButtonText: "Przejdź do bramki płatności..."),

        [RedsysLanguage.Galician] = new(
            PageTitle: "Conectando coa pasarela de pagamento...",
            Heading: "Conectando con Redsys",
            Subheading: "Por favor, agarde un intre mentres o transferimos de forma segura ao portal bancario.",
            OrderLabel: "Pedido:",
            AmountLabel: "Importe:",
            DefaultButtonText: "Continuar á pasarela de pagamento..."),

        [RedsysLanguage.Basque] = new(
            PageTitle: "Ordainketa-pasabidearekin konektatzen...",
            Heading: "Redsysekin konektatzen",
            Subheading: "Mesedez, itxaron une bat banku-atarira modu seguruan bideratzen zaitugun bitartean.",
            OrderLabel: "Eskaera:",
            AmountLabel: "Zenbatekoa:",
            DefaultButtonText: "Jarraitu ordainketa-pasabidera...")
    }.ToFrozenDictionary();

    /// <summary>
    /// Gets the localized HTML texts for a given <see cref="RedsysLanguage"/>.
    /// </summary>
    public static RedsysHtmlTexts GetHtmlTexts(RedsysLanguage language)
    {
        if (HtmlTexts.TryGetValue(language, out var texts))
        {
            return texts;
        }

        return HtmlTexts[RedsysLanguage.Spanish];
    }
}
