using System.Collections.Frozen;
using System.Globalization;

namespace BetterRedsys.Models;

/// <summary>
/// Official catalog of response codes returned by the Redsys SIS processing gateway with comprehensive multi-language support (13 languages).
/// </summary>
public static class RedsysResponseCatalog
{
    private static readonly FrozenDictionary<RedsysLanguage, FrozenDictionary<string, string>> Catalogs = new Dictionary<RedsysLanguage, FrozenDictionary<string, string>>
    {
        [RedsysLanguage.Spanish] = CreateSpanishDictionary(),
        [RedsysLanguage.English] = CreateEnglishDictionary(),
        [RedsysLanguage.Catalan] = CreateCatalanDictionary(),
        [RedsysLanguage.French] = CreateFrenchDictionary(),
        [RedsysLanguage.German] = CreateGermanDictionary(),
        [RedsysLanguage.Dutch] = CreateDutchDictionary(),
        [RedsysLanguage.Italian] = CreateItalianDictionary(),
        [RedsysLanguage.Swedish] = CreateSwedishDictionary(),
        [RedsysLanguage.Portuguese] = CreatePortugueseDictionary(),
        [RedsysLanguage.Valencian] = CreateValencianDictionary(),
        [RedsysLanguage.Polish] = CreatePolishDictionary(),
        [RedsysLanguage.Galician] = CreateGalicianDictionary(),
        [RedsysLanguage.Basque] = CreateBasqueDictionary()
    }.ToFrozenDictionary();

    /// <summary>
    /// Checks whether a Redsys numeric response code represents a successful payment (between 0000 and 0099, or 0900 for refunds).
    /// </summary>
    /// <param name="responseCode">The code returned in Ds_Response.</param>
    /// <returns>True if authorized; otherwise false.</returns>
    public static bool IsSuccess(string? responseCode)
    {
        if (string.IsNullOrWhiteSpace(responseCode))
        {
            return false;
        }

        if (int.TryParse(responseCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
        {
            // Standard authorized payments (0 to 99)
            if (code is >= 0 and <= 99)
            {
                return true;
            }

            // Authorized refund (900) or cancellation (400)
            if (code is 900 or 400)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a human-readable description of the Redsys response code in the requested language (defaults to Spanish).
    /// </summary>
    /// <param name="responseCode">The code returned in Ds_Response.</param>
    /// <param name="language">Target language enum.</param>
    /// <returns>A localized description string.</returns>
    public static string GetDescription(string? responseCode, RedsysLanguage language = RedsysLanguage.Spanish)
    {
        if (!Catalogs.TryGetValue(language, out var dict))
        {
            dict = Catalogs[RedsysLanguage.Spanish];
        }

        if (string.IsNullOrWhiteSpace(responseCode))
        {
            return GetMissingCodeText(language);
        }

        // Direct dictionary match
        if (dict.TryGetValue(responseCode, out var description))
        {
            return description;
        }

        // Fallback to numeric ranges
        if (int.TryParse(responseCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
        {
            return GetRangeFallbackText(code, responseCode, language);
        }

        return GetUnknownCodeText(responseCode, language);
    }

    /// <summary>
    /// Gets a human-readable description of the Redsys response code using the provided <see cref="CultureInfo"/>.
    /// </summary>
    public static string GetDescription(string? responseCode, CultureInfo? culture)
    {
        var language = RedsysLanguageExtensions.FromCultureInfo(culture);
        return GetDescription(responseCode, language);
    }

    private static string GetMissingCodeText(RedsysLanguage language) => language switch
    {
        RedsysLanguage.Spanish => "Código de respuesta ausente o no especificado.",
        RedsysLanguage.English => "Response code missing or unspecified.",
        RedsysLanguage.Catalan => "Codi de resposta absent o no especificat.",
        RedsysLanguage.French => "Code de réponse manquant ou non spécifié.",
        RedsysLanguage.German => "Antwortcode fehlt oder nicht angegeben.",
        RedsysLanguage.Dutch => "Responscode ontbreekt of is niet gespecificeerd.",
        RedsysLanguage.Italian => "Codice di risposta mancante o non specificato.",
        RedsysLanguage.Swedish => "Svarskod saknas eller är ospecificerad.",
        RedsysLanguage.Portuguese => "Código de resposta ausente ou não especificado.",
        RedsysLanguage.Valencian => "Codi de resposta absent o no especificat.",
        RedsysLanguage.Polish => "Brakujący lub nieokreślony kod odpowiedzi.",
        RedsysLanguage.Galician => "Código de resposta ausente ou non especificado.",
        RedsysLanguage.Basque => "Erantzun-kodea falta da edo zehaztu gabe dago.",
        _ => "Código de respuesta ausente o no especificado."
    };

    private static string GetUnknownCodeText(string code, RedsysLanguage language) => language switch
    {
        RedsysLanguage.Spanish => $"Respuesta denegada o desconocida de Redsys (Código: {code}).",
        RedsysLanguage.English => $"Declined or unknown Redsys response (Code: {code}).",
        RedsysLanguage.Catalan => $"Resposta denegada o desconeguda de Redsys (Codi: {code}).",
        RedsysLanguage.French => $"Réponse refusée ou inconnue de Redsys (Code : {code}).",
        RedsysLanguage.German => $"Abgelehnte oder unbekannte Antwort von Redsys (Code: {code}).",
        RedsysLanguage.Dutch => $"Geweigerde of onbekende reactie van Redsys (Code: {code}).",
        RedsysLanguage.Italian => $"Risposta rifiutata o sconosciuta da Redsys (Codice: {code}).",
        RedsysLanguage.Swedish => $"Nekat eller okänt svar från Redsys (Kod: {code}).",
        RedsysLanguage.Portuguese => $"Resposta recusada ou desconhecida de Redsys (Código: {code}).",
        RedsysLanguage.Valencian => $"Resposta denegada o desconeguda de Redsys (Codi: {code}).",
        RedsysLanguage.Polish => $"Odrzucona lub nieznana odpowiedź Redsys (Kod: {code}).",
        RedsysLanguage.Galician => $"Resposta denegada ou descoñecida de Redsys (Código: {code}).",
        RedsysLanguage.Basque => $"Redsysen erantzun ukatua edo ezezaguna (Kodea: {code}).",
        _ => $"Respuesta denegada o desconocida de Redsys (Código: {code})."
    };

    private static string GetRangeFallbackText(int numericCode, string rawCode, RedsysLanguage language)
    {
        if (numericCode is >= 0 and <= 99)
        {
            return language switch
            {
                RedsysLanguage.Spanish => $"Transacción autorizada con éxito (Código {rawCode}).",
                RedsysLanguage.English => $"Transaction authorized successfully (Code {rawCode}).",
                RedsysLanguage.Catalan => $"Transacció autoritzada amb èxit (Codi {rawCode}).",
                RedsysLanguage.French => $"Transaction autorisée avec succès (Code {rawCode}).",
                RedsysLanguage.German => $"Transaktion erfolgreich autorisiert (Code {rawCode}).",
                RedsysLanguage.Dutch => $"Transactie succesvol geautoriseerd (Code {rawCode}).",
                RedsysLanguage.Italian => $"Transazione autorizzata con successo (Codice {rawCode}).",
                RedsysLanguage.Swedish => $"Transaktionen har auktoriserats framgångsrikt (Kod {rawCode}).",
                RedsysLanguage.Portuguese => $"Transação autorizada com sucesso (Código {rawCode}).",
                RedsysLanguage.Valencian => $"Transacció autoritzada amb èxit (Codi {rawCode}).",
                RedsysLanguage.Polish => $"Transakcja autoryzowana pomyślnie (Kod {rawCode}).",
                RedsysLanguage.Galician => $"Transacción autorizada con éxito (Código {rawCode}).",
                RedsysLanguage.Basque => $"Transakzioa arrakastaz baimendu da (Kodea {rawCode}).",
                _ => $"Transacción autorizada con éxito (Código {rawCode})."
            };
        }

        if (numericCode is >= 100 and <= 199)
        {
            return language switch
            {
                RedsysLanguage.Spanish => $"Operación denegada por la entidad emisora (Código {rawCode}).",
                RedsysLanguage.English => $"Transaction declined by issuing entity (Code {rawCode}).",
                RedsysLanguage.Catalan => $"Operació denegada per l'entitat emissora (Codi {rawCode}).",
                RedsysLanguage.French => $"Opération refusée par la banque émettrice (Code {rawCode}).",
                RedsysLanguage.German => $"Vorgang von der ausstellenden Bank abgelehnt (Code {rawCode}).",
                RedsysLanguage.Dutch => $"Transactie geweigerd door uitgevende instantie (Code {rawCode}).",
                RedsysLanguage.Italian => $"Operazione rifiutata dall'istituto emittente (Codice {rawCode}).",
                RedsysLanguage.Swedish => $"Transaktionen avvisades av kortutgivaren (Kod {rawCode}).",
                RedsysLanguage.Portuguese => $"Operação recusada pela entidade emissora (Código {rawCode}).",
                RedsysLanguage.Valencian => $"Operació denegada per l'entitat emissora (Codi {rawCode}).",
                RedsysLanguage.Polish => $"Transakcja odrzucona przez bank wystawcy (Kod {rawCode}).",
                RedsysLanguage.Galician => $"Operación denegada pola entidade emisora (Código {rawCode}).",
                RedsysLanguage.Basque => $"Eragiketa entitate jaulkitzaileak ukatu du (Kodea {rawCode}).",
                _ => $"Operación denegada por la entidad emisora (Código {rawCode})."
            };
        }

        if (numericCode is >= 200 and <= 299)
        {
            return language switch
            {
                RedsysLanguage.Spanish => $"Operación denegada con orden de retención de tarjeta (Código {rawCode}).",
                RedsysLanguage.English => $"Transaction declined with card retention order (Code {rawCode}).",
                RedsysLanguage.Catalan => $"Operació denegada amb ordre de retenció de targeta (Codi {rawCode}).",
                RedsysLanguage.French => $"Opération refusée avec ordre de rétention de la carte (Code {rawCode}).",
                RedsysLanguage.German => $"Vorgang mit Einzugsanweisung abgelehnt (Code {rawCode}).",
                RedsysLanguage.Dutch => $"Transactie geweigerd met bevel tot inhouding van de kaart (Code {rawCode}).",
                RedsysLanguage.Italian => $"Operazione rifiutata con ordine di trattenuta della carta (Codice {rawCode}).",
                RedsysLanguage.Swedish => $"Transaktionen avvisades med order om kortindragning (Kod {rawCode}).",
                RedsysLanguage.Portuguese => $"Operação recusada com ordem de retenção de cartão (Código {rawCode}).",
                RedsysLanguage.Valencian => $"Operació denegada amb orde de retenció de targeta (Codi {rawCode}).",
                RedsysLanguage.Polish => $"Transakcja odrzucona z nakazem zatrzymania karty (Kod {rawCode}).",
                RedsysLanguage.Galician => $"Operación denegada con orde de retención de tarxeta (Código {rawCode}).",
                RedsysLanguage.Basque => $"Eragiketa txartela atxikitzeko aginduarekin ukatu da (Kodea {rawCode}).",
                _ => $"Operación denegada con orden de retención de tarjeta (Código {rawCode})."
            };
        }

        if (numericCode is >= 900 and <= 999)
        {
            return language switch
            {
                RedsysLanguage.Spanish => $"Error técnico o de validación en la pasarela Redsys (Código {rawCode}).",
                RedsysLanguage.English => $"Technical or validation error in Redsys gateway (Code {rawCode}).",
                RedsysLanguage.Catalan => $"Error tècnic o de validació a la passarel·la Redsys (Codi {rawCode}).",
                RedsysLanguage.French => $"Erreur technique ou de validation sur la passerelle Redsys (Code {rawCode}).",
                RedsysLanguage.German => $"Technischer oder Validierungsfehler im Redsys-Gateway (Code {rawCode}).",
                RedsysLanguage.Dutch => $"Technische of validatiefout in Redsys-gateway (Code {rawCode}).",
                RedsysLanguage.Italian => $"Errore tecnico o di validazione nel gateway Redsys (Codice {rawCode}).",
                RedsysLanguage.Swedish => $"Tekniskt eller valideringsfel i Redsys-gateway (Kod {rawCode}).",
                RedsysLanguage.Portuguese => $"Erro técnico ou de validação na pasarela Redsys (Código {rawCode}).",
                RedsysLanguage.Valencian => $"Error tècnic o de validació en la passarel·la Redsys (Codi {rawCode}).",
                RedsysLanguage.Polish => $"Błąd techniczny lub walidacji w bramce Redsys (Kod {rawCode}).",
                RedsysLanguage.Galician => $"Erro técnico ou de validación na pasarela Redsys (Código {rawCode}).",
                RedsysLanguage.Basque => $"Errore teknikoa edo baliozkotze-errorea Redsys pasabidean (Kodea {rawCode}).",
                _ => $"Error técnico o de validación en la pasarela Redsys (Código {rawCode})."
            };
        }

        return GetUnknownCodeText(rawCode, language);
    }

    private static FrozenDictionary<string, string> CreateSpanishDictionary() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["0000"] = "Transacción autorizada con éxito.",
        ["0900"] = "Devolución o confirmación autorizada con éxito.",
        ["0400"] = "Anulación autorizada con éxito.",
        ["0101"] = "Tarjeta caducada.",
        ["0102"] = "Tarjeta en excepción transitoria o bajo sospecha de fraude.",
        ["0104"] = "Operación no permitida para ese tipo de tarjeta.",
        ["0106"] = "Número de intentos de PIN excedido.",
        ["0116"] = "Disponible o saldo insuficiente.",
        ["0118"] = "Tarjeta no registrada o no operativa.",
        ["0125"] = "Tarjeta no efectiva.",
        ["0129"] = "Código de seguridad (CVV2/CVC2) incorrecto.",
        ["0180"] = "Tarjeta ajena al servicio o denegada por entidad.",
        ["0184"] = "Error en la autenticación del titular (fallo en 3D Secure / SCA / SMS OTP).",
        ["0190"] = "Denegación del emisor sin especificar motivo.",
        ["0191"] = "Fecha de caducidad errónea.",
        ["0202"] = "Tarjeta en excepción transitoria o sospecha de fraude.",
        ["0904"] = "Comercio no registrado en FUC o terminal inactivo.",
        ["0912"] = "Centro emisor no disponible (entidad bancaria del cliente inaccesible).",
        ["0913"] = "Número de pedido repetido o duplicado.",
        ["0944"] = "Sesión incorrecta o parámetros inconsistentes.",
        ["0950"] = "Operación de devolución no permitida o fuera de plazo.",
        ["9064"] = "Número de posiciones de la tarjeta incorrecto.",
        ["9093"] = "Tarjeta inexistente.",
        ["9142"] = "Tiempo de pago excedido (timeout).",
        ["9912"] = "Centro emisor no disponible.",
        ["9913"] = "Error en la confirmación de la pasarela.",
        ["9914"] = "Confirmación KO de la pasarela.",
        ["9915"] = "Transacción cancelada voluntariamente por el usuario.",
        ["9928"] = "Anulación de autorización en diferido realizada por el SIS.",
        ["9929"] = "Anulación de autorización en diferido realizada por el comercio.",
        ["9997"] = "Transacción procesándose simultáneamente en otro hilo o terminal.",
        ["9998"] = "Operación en proceso de solicitud de tarjeta.",
        ["9999"] = "Operación en proceso de autenticación del titular."
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static FrozenDictionary<string, string> CreateEnglishDictionary() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["0000"] = "Transaction authorized successfully.",
        ["0900"] = "Refund or confirmation authorized successfully.",
        ["0400"] = "Cancellation authorized successfully.",
        ["0101"] = "Card expired.",
        ["0102"] = "Card in temporary exception or suspected fraud.",
        ["0104"] = "Transaction not allowed for this card type.",
        ["0106"] = "Exceeded PIN attempt limit.",
        ["0116"] = "Insufficient funds or available balance.",
        ["0118"] = "Card not registered or inactive.",
        ["0125"] = "Card not effective.",
        ["0129"] = "Incorrect security code (CVV2/CVC2).",
        ["0180"] = "Card not recognized by service or declined by card issuer.",
        ["0184"] = "Cardholder authentication error (3D Secure / SCA / SMS OTP failure).",
        ["0190"] = "Issuer declined transaction without specifying reason.",
        ["0191"] = "Invalid card expiration date.",
        ["0202"] = "Card in temporary exception or suspected fraud.",
        ["0904"] = "Merchant not registered in FUC or terminal inactive.",
        ["0912"] = "Issuer host unavailable (customer's bank system unreachable).",
        ["0913"] = "Duplicate or repeated order number.",
        ["0944"] = "Invalid session or inconsistent parameters.",
        ["0950"] = "Refund transaction not permitted or out of timeframe.",
        ["9064"] = "Invalid number of card digits.",
        ["9093"] = "Card does not exist.",
        ["9142"] = "Payment session expired (timeout).",
        ["9912"] = "Issuer host unavailable.",
        ["9913"] = "Gateway confirmation error.",
        ["9914"] = "Gateway KO confirmation.",
        ["9915"] = "Transaction canceled voluntarily by the user.",
        ["9928"] = "Deferred authorization cancellation performed by SIS.",
        ["9929"] = "Deferred authorization cancellation performed by merchant.",
        ["9997"] = "Transaction being processed simultaneously in another thread or terminal.",
        ["9998"] = "Operation in process of card request.",
        ["9999"] = "Operation in process of cardholder authentication."
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static FrozenDictionary<string, string> CreateCatalanDictionary() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["0000"] = "Transacció autoritzada amb èxit.",
        ["0900"] = "Devolució o confirmació autoritzada amb èxit.",
        ["0400"] = "Anul·lació autoritzada amb èxit.",
        ["0101"] = "Targeta caducada.",
        ["0102"] = "Targeta en excepció transitòria o sota sospita de frau.",
        ["0104"] = "Operació no permesa per a aquest tipus de targeta.",
        ["0106"] = "Nombre d'intents de PIN superat.",
        ["0116"] = "Saldo o disponible insuficient.",
        ["0118"] = "Targeta no registrada o no operativa.",
        ["0125"] = "Targeta no efectiva.",
        ["0129"] = "Codi de seguretat (CVV2/CVC2) incorrecte.",
        ["0180"] = "Targeta aliena al servei o denegada per l'entitat.",
        ["0184"] = "Error en l'autenticació del titular (fallada en 3D Secure / SCA / SMS OTP).",
        ["0190"] = "Denegació de l'emissor sense especificar motiu.",
        ["0191"] = "Data de caducitat errònia.",
        ["0202"] = "Targeta en excepció transitòria o sospita de frau.",
        ["0904"] = "Comerç no registrat a FUC o terminal inactiu.",
        ["0912"] = "Centre emissor no disponible (entitat bancària del client inaccessible).",
        ["0913"] = "Número de comanda repetit o duplicat.",
        ["0944"] = "Sessió incorrecta o paràmetres inconsistents.",
        ["0950"] = "Operació de devolució no permesa o fora de termini.",
        ["9064"] = "Nombre de posicions de la targeta incorrecte.",
        ["9093"] = "Targeta inexistent.",
        ["9142"] = "Temps de pagament superat (temps d'espera esgotat).",
        ["9912"] = "Centre emissor no disponible.",
        ["9913"] = "Error en la confirmació de la passarel·la.",
        ["9914"] = "Confirmació KO de la passarel·la.",
        ["9915"] = "Transacció cancel·lada voluntàriament per l'usuari.",
        ["9928"] = "Anul·lació d'autorització en diferit realitzada pel SIS.",
        ["9929"] = "Anul·lació d'autorització en diferit realitzada pel comerç.",
        ["9997"] = "Transacció processant-se simultàniament en un altre fil o terminal.",
        ["9998"] = "Operació en procés de sol·licitud de targeta.",
        ["9999"] = "Operació en procés d'autenticació del titular."
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static FrozenDictionary<string, string> CreateFrenchDictionary() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["0000"] = "Transaction autorisée avec succès.",
        ["0900"] = "Remboursement ou confirmation autorisé avec succès.",
        ["0400"] = "Annulation autorisée avec succès.",
        ["0101"] = "Carte expirée.",
        ["0102"] = "Carte en exception temporaire ou soupçon de fraude.",
        ["0104"] = "Opération non autorisée pour ce type de carte.",
        ["0106"] = "Nombre d'essais de code PIN dépassé.",
        ["0116"] = "Solde ou fonds insuffisants.",
        ["0118"] = "Carte non enregistrée ou inactive.",
        ["0125"] = "Carte non valide.",
        ["0129"] = "Code de sécurité (CVV2/CVC2) incorrect.",
        ["0180"] = "Carte non reconnue ou refusée par la banque émettrice.",
        ["0184"] = "Erreur d'authentification du titulaire (échec 3D Secure / SCA / SMS OTP).",
        ["0190"] = "Refus de l'émetteur sans motif précisé.",
        ["0191"] = "Date d'expiration erronée.",
        ["0202"] = "Carte en exception temporaire ou suspicion de fraude.",
        ["0904"] = "Commerçant non enregistré dans le FUC ou terminal inactif.",
        ["0912"] = "Centre émetteur indisponible (banque du client inaccessible).",
        ["0913"] = "Numéro de commande répété ou dupliqué.",
        ["0944"] = "Session incorrecte ou paramètres incohérents.",
        ["0950"] = "Opération de remboursement non autorisée ou délai dépassé.",
        ["9064"] = "Nombre de chiffres de carte incorrect.",
        ["9093"] = "Carte inexistante.",
        ["9142"] = "Délai de paiement dépassé (expiration).",
        ["9912"] = "Centre émetteur indisponible.",
        ["9913"] = "Erreur de confirmation de la passerelle.",
        ["9914"] = "Confirmation KO de la passerelle.",
        ["9915"] = "Transaction annulée volontairement par l'utilisateur.",
        ["9928"] = "Annulation d'autorisation différée effectuée par le SIS.",
        ["9929"] = "Annulation d'autorisation différée effectuée par le commerçant.",
        ["9997"] = "Transaction en cours de traitement simultané.",
        ["9998"] = "Opération en cours de demande de carte.",
        ["9999"] = "Opération en cours d'authentification du titulaire."
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static FrozenDictionary<string, string> CreateGermanDictionary() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["0000"] = "Transaktion erfolgreich autorisiert.",
        ["0900"] = "Rückerstattung oder Bestätigung erfolgreich autorisiert.",
        ["0400"] = "Stornierung erfolgreich autorisiert.",
        ["0101"] = "Karte abgelaufen.",
        ["0102"] = "Karte vorübergehend gesperrt oder Betrugsverdacht.",
        ["0104"] = "Vorgang für diesen Kartentyp nicht zulässig.",
        ["0106"] = "Maximale Anzahl an PIN-Versuchen überschritten.",
        ["0116"] = "Unzureichender Saldo oder Deckung.",
        ["0118"] = "Karte nicht registriert oder inaktiv.",
        ["0125"] = "Karte ungültig.",
        ["0129"] = "Sicherheitscode (CVV2/CVC2) falsch.",
        ["0180"] = "Karte vom Dienst nicht erkannt oder von Bank abgelehnt.",
        ["0184"] = "Authentifizierungsfehler des Karteninhabers (3D Secure / SCA / SMS OTP fehlgeschlagen).",
        ["0190"] = "Ablehnung durch kartenausgebende Bank ohne Grundangabe.",
        ["0191"] = "Ablaufdatum ungültig.",
        ["0202"] = "Karte vorübergehend gesperrt oder Betrugsverdacht mit Karteneinzug.",
        ["0904"] = "Händler nicht registriert oder Terminal inaktiv.",
        ["0912"] = "Kartenausgebende Bank nicht erreichbar.",
        ["0913"] = "Bestellnummer wiederholt oder dupliziert.",
        ["0944"] = "Ungültige Sitzung oder inkonsistente Parameter.",
        ["0950"] = "Rückerstattung nicht zulässig oder Frist abgelaufen.",
        ["9064"] = "Ungültige Anzahl von Ziffern auf der Karte.",
        ["9093"] = "Karte existiert nicht.",
        ["9142"] = "Zahlungszeitlimit überschritten (Timeout).",
        ["9912"] = "Kartenausgebende Bank nicht verfügbar.",
        ["9913"] = "Gateway-Bestätigungsfehler.",
        ["9914"] = "Gateway KO-Bestätigung.",
        ["9915"] = "Transaktion vom Benutzer freiwillig abgebrochen.",
        ["9928"] = "Verzögerte Autorisierungsstornierung durch SIS durchgeführt.",
        ["9929"] = "Verzögerte Autorisierungsstornierung durch Händler durchgeführt.",
        ["9997"] = "Transaktion wird gleichzeitig verarbeitet.",
        ["9998"] = "Vorgang in Bearbeitung der Kartenanforderung.",
        ["9999"] = "Vorgang in Bearbeitung der Karteninhaber-Authentifizierung."
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static FrozenDictionary<string, string> CreateDutchDictionary() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["0000"] = "Transactie succesvol geautoriseerd.",
        ["0900"] = "Terugbetaling of bevestiging succesvol geautoriseerd.",
        ["0400"] = "Annulering succesvol geautoriseerd.",
        ["0101"] = "Kaart verlopen.",
        ["0102"] = "Kaart tijdelijk geblokkeerd of vermoeden van fraude.",
        ["0104"] = "Transactie niet toegestaan voor dit kaarttype.",
        ["0106"] = "Aantal pincodepogingen overschreden.",
        ["0116"] = "Onvoldoende saldo of krediet.",
        ["0118"] = "Kaart niet geregistreerd of inactief.",
        ["0125"] = "Kaart niet geldig.",
        ["0129"] = "Beveiligingscode (CVV2/CVC2) onjuist.",
        ["0180"] = "Kaart niet herkend of geweigerd door de bank.",
        ["0184"] = "Authenticatiefout kaarthouder (3D Secure / SCA / SMS OTP mislukt).",
        ["0190"] = "Geweigerd door verstrekker zonder opgaaf van reden.",
        ["0191"] = "Vervaldatum onjuist.",
        ["0202"] = "Kaart in tijdelijke uitzondering of vermoeden van fraude.",
        ["0904"] = "Handelaar niet geregistreerd of terminal inactief.",
        ["0912"] = "Bank van kaarthouder niet beschikbaar.",
        ["0913"] = "Bestelnummer herhaald of gedupliceerd.",
        ["0944"] = "Ongeldige sessie of inconsistente parameters.",
        ["0950"] = "Terugbetalingstransactie niet toegestaan of termijn verstreken.",
        ["9064"] = "Onjuist aantal cijfers op de kaart.",
        ["9093"] = "Kaart bestaat niet.",
        ["9142"] = "Betalingssessie verlopen (time-out).",
        ["9912"] = "Bank van kaarthouder niet beschikbaar.",
        ["9913"] = "Gateway-bevestigingsfout.",
        ["9914"] = "Gateway KO-bevestiging.",
        ["9915"] = "Transactie vrijwillig geannuleerd door de gebruiker.",
        ["9928"] = "Uitgestelde autorisatieannulering uitgevoerd door SIS.",
        ["9929"] = "Uitgestelde autorisatieannulering uitgevoerd door handelaar.",
        ["9997"] = "Transactie wordt gelijktijdig verwerkt.",
        ["9998"] = "Kaartaanvraag in behandeling.",
        ["9999"] = "Kaarthouderauthenticatie in behandeling."
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static FrozenDictionary<string, string> CreateItalianDictionary() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["0000"] = "Transazione autorizzata con successo.",
        ["0900"] = "Rimborso o conferma autorizzato con successo.",
        ["0400"] = "Annullamento autorizzato con successo.",
        ["0101"] = "Carta scaduta.",
        ["0102"] = "Carta in eccezione temporanea o sospetta frode.",
        ["0104"] = "Operazione non consentita per questo tipo di carta.",
        ["0106"] = "Numero massimo di tentativi PIN superato.",
        ["0116"] = "Fondi o saldo disponibile insufficiente.",
        ["0118"] = "Carta non registrata o non operativa.",
        ["0125"] = "Carta non attiva.",
        ["0129"] = "Codice di sicurezza (CVV2/CVC2) errato.",
        ["0180"] = "Carta non riconosciuta o rifiutata dall'istituto emittente.",
        ["0184"] = "Errore di autenticazione del titolare (fallimento 3D Secure / SCA / SMS OTP).",
        ["0190"] = "Rifiuto dell'emittente senza specificare il motivo.",
        ["0191"] = "Data di scadenza errata.",
        ["0202"] = "Carta in eccezione temporanea o sospetta frode con ritiro carta.",
        ["0904"] = "Esercente non registrato nel FUC o terminale inattivo.",
        ["0912"] = "Istituto emittente non disponibile (banca del cliente irraggiungibile).",
        ["0913"] = "Numero d'ordine ripetuto o duplicato.",
        ["0944"] = "Sessione errata o parametri incoerenti.",
        ["0950"] = "Operazione di rimborso non consentita o fuori termine.",
        ["9064"] = "Numero di cifre della carta errato.",
        ["9093"] = "Carta inesistente.",
        ["9142"] = "Tempo per il pagamento scaduto (timeout).",
        ["9912"] = "Istituto emittente non disponibile.",
        ["9913"] = "Errore nella conferma del gateway.",
        ["9914"] = "Conferma KO del gateway.",
        ["9915"] = "Transazione annullata volontariamente dall'utente.",
        ["9928"] = "Annullamento autorizzazione differita eseguita dal SIS.",
        ["9929"] = "Annullamento autorizzazione differita eseguita dall'esercente.",
        ["9997"] = "Transazione in elaborazione simultanea.",
        ["9998"] = "Operazione in corso di richiesta carta.",
        ["9999"] = "Operazione in corso di autenticazione del titolare."
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static FrozenDictionary<string, string> CreateSwedishDictionary() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["0000"] = "Transaktionen har auktoriserats framgångsrikt.",
        ["0900"] = "Återbetalning eller bekräftelse har auktoriserats framgångsrikt.",
        ["0400"] = "Avbokning har auktoriserats framgångsrikt.",
        ["0101"] = "Kortet har gått ut.",
        ["0102"] = "Kort i tillfälligt undantag eller misstänkt bedrägeri.",
        ["0104"] = "Transaktionen är inte tillåten för denna korttyp.",
        ["0106"] = "Maximalt antal PIN-försök har överskridits.",
        ["0116"] = "Otillräckligt saldo eller disponibelt belopp.",
        ["0118"] = "Kortet är inte registrerat eller inaktivt.",
        ["0125"] = "Kortet är ogiltigt.",
        ["0129"] = "Felaktig säkerhetskod (CVV2/CVC2).",
        ["0180"] = "Kortet känns inte igen eller avvisades av banken.",
        ["0184"] = "Autentiseringsfel för kortinnehavaren (3D Secure / SCA / SMS OTP misslyckades).",
        ["0190"] = "Nekad av kortutgivaren utan angiven orsak.",
        ["0191"] = "Felaktigt utgångsdatum.",
        ["0202"] = "Kort i tillfälligt undantag eller misstänkt bedrägeri med kortindragning.",
        ["0904"] = "Handlaren är inte registrerad eller terminalen är inaktiv.",
        ["0912"] = "Kortutgivaren är inte tillgänglig.",
        ["0913"] = "Upprepat eller duplicerat ordernummer.",
        ["0944"] = "Ogiltig session eller inkonsekventa parametrar.",
        ["0950"] = "Återbetalningstransaktion inte tillåten eller tidsfristen har gått ut.",
        ["9064"] = "Felaktigt antal siffror på kortet.",
        ["9093"] = "Kortet finns inte.",
        ["9142"] = "Betalningssessionen har löpt ut (timeout).",
        ["9912"] = "Kortutgivaren är inte tillgänglig.",
        ["9913"] = "Fel vid gatewaybekräftelse.",
        ["9914"] = "Gateway KO-bekräftelse.",
        ["9915"] = "Transaktionen avbröts frivilligt av användaren.",
        ["9928"] = "Uppskjuten auktoriseringsavbokning utförd av SIS.",
        ["9929"] = "Uppskjuten auktoriseringsavbokning utförd av handlaren.",
        ["9997"] = "Transaktionen bearbetas samtidigt i en annan tråd eller terminal.",
        ["9998"] = "Kortbegäran pågår.",
        ["9999"] = "Kortinnehavarautentisering pågår."
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static FrozenDictionary<string, string> CreatePortugueseDictionary() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["0000"] = "Transação autorizada com sucesso.",
        ["0900"] = "Reembolso ou confirmação autorizada com sucesso.",
        ["0400"] = "Anulação autorizada com sucesso.",
        ["0101"] = "Cartão caducado.",
        ["0102"] = "Cartão em exceção transitória ou sob suspeita de fraude.",
        ["0104"] = "Operação não permitida para este tipo de cartão.",
        ["0106"] = "Número de tentativas de PIN excedido.",
        ["0116"] = "Saldo ou montante disponível insuficiente.",
        ["0118"] = "Cartão não registado ou não operacional.",
        ["0125"] = "Cartão inválido.",
        ["0129"] = "Código de segurança (CVV2/CVC2) incorreto.",
        ["0180"] = "Cartão não reconhecido pelo serviço ou recusado pelo emissor.",
        ["0184"] = "Erro na autenticação do titular (falha no 3D Secure / SCA / SMS OTP).",
        ["0190"] = "Recusa do emissor sem especificar motivo.",
        ["0191"] = "Data de validade incorreta.",
        ["0202"] = "Cartão em exceção transitória ou suspeita de fraude.",
        ["0904"] = "Comerciante não registado no FUC ou terminal inativo.",
        ["0912"] = "Centro emissor não disponível (banco do cliente inacessível).",
        ["0913"] = "Número de encomenda repetido ou duplicado.",
        ["0944"] = "Sessão incorreta ou parâmetros inconsistentes.",
        ["0950"] = "Operação de reembolso não permitida ou fora do prazo.",
        ["9064"] = "Número de posições do cartão incorreto.",
        ["9093"] = "Cartão inexistente.",
        ["9142"] = "Tempo de pagamento excedido (tempo limite esgotado).",
        ["9912"] = "Centro emissor não disponível.",
        ["9913"] = "Erro na confirmação da pasarela.",
        ["9914"] = "Confirmação KO da pasarela.",
        ["9915"] = "Transação cancelada voluntariamente pelo utilizador.",
        ["9928"] = "Anulação de autorização diferida realizada pelo SIS.",
        ["9929"] = "Anulação de autorização diferida realizada pelo comerciante.",
        ["9997"] = "Transação a ser processada simultaneamente.",
        ["9998"] = "Operação em processo de solicitação de cartão.",
        ["9999"] = "Operação em processo de autenticação do titular."
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static FrozenDictionary<string, string> CreateValencianDictionary() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["0000"] = "Transacció autoritzada amb èxit.",
        ["0900"] = "Devolució o confirmació autoritzada amb èxit.",
        ["0400"] = "Anul·lació autoritzada amb èxit.",
        ["0101"] = "Targeta caducada.",
        ["0102"] = "Targeta en excepció transitòria o sota sospita de frau.",
        ["0104"] = "Operació no permesa per a este tipus de targeta.",
        ["0106"] = "Nombre d'intents de PIN superat.",
        ["0116"] = "Saldo o disponible insuficient.",
        ["0118"] = "Targeta no registrada o inactiva.",
        ["0125"] = "Targeta no efectiva.",
        ["0129"] = "Codi de seguretat (CVV2/CVC2) incorrecte.",
        ["0180"] = "Targeta aliena al servici o denegada per l'entitat.",
        ["0184"] = "Error en l'autenticació del titular (fallada en 3D Secure / SCA / SMS OTP).",
        ["0190"] = "Denegació de l'emissor sense especificar motiu.",
        ["0191"] = "Data de caducitat errònia.",
        ["0202"] = "Targeta en excepció transitòria o sospita de frau.",
        ["0904"] = "Comerç no registrat en FUC o terminal inactiu.",
        ["0912"] = "Centre emissor no disponible (entitat bancària del client inaccessible).",
        ["0913"] = "Número de comanda repetit o duplicat.",
        ["0944"] = "Sessió incorrecta o paràmetres inconsistents.",
        ["0950"] = "Operació de devolució no permesa o fora de termini.",
        ["9064"] = "Nombre de posicions de la targeta incorrecte.",
        ["9093"] = "Targeta inexistent.",
        ["9142"] = "Temps de pagament superat (temps d'espera esgotat).",
        ["9912"] = "Centre emissor no disponible.",
        ["9913"] = "Error en la confirmació de la passarel·la.",
        ["9914"] = "Confirmació KO de la passarel·la.",
        ["9915"] = "Transacció cancel·lada voluntàriament per l'usuari.",
        ["9928"] = "Anul·lació d'autorització en diferit realitzada pel SIS.",
        ["9929"] = "Anul·lació d'autorització en diferit realitzada pel comerç.",
        ["9997"] = "Transacció processant-se simultàniament en un altre fil o terminal.",
        ["9998"] = "Operació en procés de sol·licitud de targeta.",
        ["9999"] = "Operació en procés d'autenticació del titular."
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static FrozenDictionary<string, string> CreatePolishDictionary() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["0000"] = "Transakcja autoryzowana pomyślnie.",
        ["0900"] = "Zwrot lub potwierdzenie autoryzowane pomyślnie.",
        ["0400"] = "Anulowanie autoryzowane pomyślnie.",
        ["0101"] = "Karta utraciła ważność.",
        ["0102"] = "Karta w stanie wyjątku tymczasowego lub podejrzana o oszustwo.",
        ["0104"] = "Operacja niedozwolona dla tego typu karty.",
        ["0106"] = "Przekroczono limit prób wprowadzenia kodu PIN.",
        ["0116"] = "Niewystarczające środki lub dostępny limit.",
        ["0118"] = "Karta niezarejestrowana lub nieaktywna.",
        ["0125"] = "Karta nieważna.",
        ["0129"] = "Nieprawidłowy kod bezpieczeństwa (CVV2/CVC2).",
        ["0180"] = "Karta nierozpoznana przez usługę lub odrzucona przez bank.",
        ["0184"] = "Błąd uwierzytelniania posiadacza karty (błąd 3D Secure / SCA / SMS OTP).",
        ["0190"] = "Odrzucenie przez wystawcę bez podania przyczyny.",
        ["0191"] = "Nieprawidłowa data ważności karty.",
        ["0202"] = "Karta w stanie wyjątku tymczasowego lub podejrzenie oszustwa z zatrzymaniem karty.",
        ["0904"] = "Sprzedawca niezarejestrowany w FUC lub nieaktywny terminal.",
        ["0912"] = "Bank wystawcy niedostępny (system bankowy klienta nie odpowiada).",
        ["0913"] = "Powtórzony lub zduplikowany numer zamówienia.",
        ["0944"] = "Nieprawidłowa sesja lub niespójne parametry.",
        ["0950"] = "Operacja zwrotu niedozwolona lub przekroczono termin.",
        ["9064"] = "Nieprawidłowa liczba cyfr na karcie.",
        ["9093"] = "Karta nie istnieje.",
        ["9142"] = "Przekroczono limit czasu płatności (timeout).",
        ["9912"] = "Bank wystawcy niedostępny.",
        ["9913"] = "Błąd potwierdzenia bramki.",
        ["9914"] = "Potwierdzenie negatywne (KO) bramki.",
        ["9915"] = "Transakcja anulowana dobrowolnie przez użytkownika.",
        ["9928"] = "Anulowanie odroczonej autoryzacji wykonane przez SIS.",
        ["9929"] = "Anulowanie odroczonej autoryzacji wykonane przez sprzedawcę.",
        ["9997"] = "Transakcja jest przetwarzana jednocześnie w innym wątku lub terminalu.",
        ["9998"] = "Operacja w trakcie żądania karty.",
        ["9999"] = "Operacja w trakcie uwierzytelniania posiadacza karty."
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static FrozenDictionary<string, string> CreateGalicianDictionary() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["0000"] = "Transacción autorizada con éxito.",
        ["0900"] = "Devolución ou confirmación autorizada con éxito.",
        ["0400"] = "Anulación autorizada con éxito.",
        ["0101"] = "Tarxeta caducada.",
        ["0102"] = "Tarxeta en excepción transitoria ou baixo sospeita de fraude.",
        ["0104"] = "Operación non permitida para este tipo de tarxeta.",
        ["0106"] = "Número de intentos de PIN superado.",
        ["0116"] = "Saldo ou dispoñible insuficiente.",
        ["0118"] = "Tarxeta non rexistrada ou non operativa.",
        ["0125"] = "Tarxeta non efectiva.",
        ["0129"] = "Código de seguridade (CVV2/CVC2) incorrecto.",
        ["0180"] = "Tarxeta allea ao servizo ou denegada pola entidade.",
        ["0184"] = "Erro na autenticación do titular (fallo en 3D Secure / SCA / SMS OTP).",
        ["0190"] = "Denegación do emisor sen especificar motivo.",
        ["0191"] = "Data de caducidade errónea.",
        ["0202"] = "Tarxeta en excepción transitoria ou sospeita de fraude.",
        ["0904"] = "Comercio non rexistrado no FUC ou terminal inactivo.",
        ["0912"] = "Centro emisor non dispoñible (entidade bancaria do cliente inaccesible).",
        ["0913"] = "Número de pedido repetido ou duplicado.",
        ["0944"] = "Sesión incorrecta ou parámetros inconsistentes.",
        ["0950"] = "Operación de devolución non permitida ou fóra de prazo.",
        ["9064"] = "Número de posicións da tarxeta incorrecto.",
        ["9093"] = "Tarxeta inexistente.",
        ["9142"] = "Tempo de pagamento superado (tempo esgotado).",
        ["9912"] = "Centro emisor non dispoñible.",
        ["9913"] = "Erro na confirmación da pasarela.",
        ["9914"] = "Confirmación KO da pasarela.",
        ["9915"] = "Transacción cancelada voluntariamente polo usuario.",
        ["9928"] = "Anulación de autorización diferida realizada polo SIS.",
        ["9929"] = "Anulación de autorización diferida realizada polo comercio.",
        ["9997"] = "Transacción procesándose simultaneamente noutro fío ou terminal.",
        ["9998"] = "Operación en proceso de solicitude de tarxeta.",
        ["9999"] = "Operación en proceso de autenticación do titular."
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static FrozenDictionary<string, string> CreateBasqueDictionary() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["0000"] = "Transakzioa arrakastaz baimendu da.",
        ["0900"] = "Itzulketa edo berrespena arrakastaz baimendu da.",
        ["0400"] = "Ezeztapena arrakastaz baimendu da.",
        ["0101"] = "Txartela iraungita dago.",
        ["0102"] = "Txartela aldi baterako salbuespenean edo iruzur susmopean.",
        ["0104"] = "Eragiketa ez da onartzen txartel mota honetarako.",
        ["0106"] = "PIN saiakera kopurua gainditu da.",
        ["0116"] = "Saldoa edo zenbateko erabilgarria ez da nahikoa.",
        ["0118"] = "Txartela ez dago erregistratuta edo ez dago erabilgarri.",
        ["0125"] = "Txartela ez da baliozkoa.",
        ["0129"] = "Segurtasun kodea (CVV2/CVC2) okerra da.",
        ["0180"] = "Txartela ez da zerbitzuan onartzen edo entitateak ukatu du.",
        ["0184"] = "Errorea titularraren autentifikazioan (3D Secure / SCA / SMS OTP huts egin du).",
        ["0190"] = "Irauleak ukatua arrazoirik zehaztu gabe.",
        ["0191"] = "Iraungitze data okerra.",
        ["0202"] = "Txartela aldi baterako salbuespenean edo iruzur susmopean, txartela atxikitzeko aginduarekin.",
        ["0904"] = "Saltokia ez dago FUC-n erregistratuta edo terminala ez dago aktibo.",
        ["0912"] = "Entitate jaulkitzailea ez dago erabilgarri (bezeroaren bankura ezin da iritsi).",
        ["0913"] = "Eskaera zenbakia errepikatua edo bikoiztua.",
        ["0944"] = "Saio okerra edo parametro ez-koherenteak.",
        ["0950"] = "Itzulketa eragiketa ez dago baimenduta edo epemuga igaro da.",
        ["9064"] = "Txarteleko zenbaki kopurua okerra da.",
        ["9093"] = "Txartela ez da existitzen.",
        ["9142"] = "Ordaintzeko denbora-muga gainditu da (denbora agortu da).",
        ["9912"] = "Entitate jaulkitzailea ez dago erabilgarri.",
        ["9913"] = "Errorea pasabidearen berrespenean.",
        ["9914"] = "Pasabidearen KO berrespena.",
        ["9915"] = "Erabiltzaileak borondatez bertan behera utzitako transakzioa.",
        ["9928"] = "SISek egindako baimen geratuaren ezeztapena.",
        ["9929"] = "Saltokiak egindako baimen geratuaren ezeztapena.",
        ["9997"] = "Transakzioa aldi berean beste hari edo terminal batean prozesatzen ari da.",
        ["9998"] = "Eragiketa txartela eskatzeko prozesuan dago.",
        ["9999"] = "Eragiketa titularra autentifikatzeko prozesuan dago."
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
}
