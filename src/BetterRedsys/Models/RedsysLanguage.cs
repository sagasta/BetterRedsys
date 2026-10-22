using System.Globalization;

namespace BetterRedsys.Models;

/// <summary>
/// Supported Redsys consumer languages according to the official Redsys SIS specification.
/// </summary>
public enum RedsysLanguage
{
    /// <summary>Spanish (code "001")</summary>
    Spanish = 1,

    /// <summary>English (code "002")</summary>
    English = 2,

    /// <summary>Catalan (code "003")</summary>
    Catalan = 3,

    /// <summary>French (code "004")</summary>
    French = 4,

    /// <summary>German (code "005")</summary>
    German = 5,

    /// <summary>Dutch (code "006")</summary>
    Dutch = 6,

    /// <summary>Italian (code "007")</summary>
    Italian = 7,

    /// <summary>Swedish (code "008")</summary>
    Swedish = 8,

    /// <summary>Portuguese (code "009")</summary>
    Portuguese = 9,

    /// <summary>Valencian (code "010")</summary>
    Valencian = 10,

    /// <summary>Polish (code "011")</summary>
    Polish = 11,

    /// <summary>Galician (code "012")</summary>
    Galician = 12,

    /// <summary>Basque (code "013")</summary>
    Basque = 13
}

/// <summary>
/// Helper extensions for <see cref="RedsysLanguage"/>.
/// </summary>
public static class RedsysLanguageExtensions
{
    /// <summary>
    /// Converts a <see cref="RedsysLanguage"/> to its official 3-digit Redsys code (e.g., "001" for Spanish, "002" for English).
    /// </summary>
    public static string ToRedsysCode(this RedsysLanguage language) => ((int)language).ToString("D3", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the ISO language code suitable for HTML lang attribute (e.g., "es", "en", "ca", "fr").
    /// </summary>
    public static string GetIsoCode(this RedsysLanguage language) => language switch
    {
        RedsysLanguage.Spanish => "es",
        RedsysLanguage.English => "en",
        RedsysLanguage.Catalan => "ca",
        RedsysLanguage.French => "fr",
        RedsysLanguage.German => "de",
        RedsysLanguage.Dutch => "nl",
        RedsysLanguage.Italian => "it",
        RedsysLanguage.Swedish => "sv",
        RedsysLanguage.Portuguese => "pt",
        RedsysLanguage.Valencian => "ca-valencia",
        RedsysLanguage.Polish => "pl",
        RedsysLanguage.Galician => "gl",
        RedsysLanguage.Basque => "eu",
        _ => "es"
    };

    /// <summary>
    /// Gets the native display name of the language (e.g., "Español", "English", "Català", "Français").
    /// </summary>
    public static string GetNativeName(this RedsysLanguage language) => language switch
    {
        RedsysLanguage.Spanish => "Español",
        RedsysLanguage.English => "English",
        RedsysLanguage.Catalan => "Català",
        RedsysLanguage.French => "Français",
        RedsysLanguage.German => "Deutsch",
        RedsysLanguage.Dutch => "Nederlands",
        RedsysLanguage.Italian => "Italiano",
        RedsysLanguage.Swedish => "Svenska",
        RedsysLanguage.Portuguese => "Português",
        RedsysLanguage.Valencian => "Valencià",
        RedsysLanguage.Polish => "Polski",
        RedsysLanguage.Galician => "Galego",
        RedsysLanguage.Basque => "Euskara",
        _ => "Español"
    };

    /// <summary>
    /// Parses an official Redsys language code into a <see cref="RedsysLanguage"/> enum. Defaults to <see cref="RedsysLanguage.Spanish"/> if unrecognized.
    /// </summary>
    public static RedsysLanguage FromRedsysCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return RedsysLanguage.Spanish;
        }

        if (int.TryParse(code, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric) &&
            Enum.IsDefined(typeof(RedsysLanguage), numeric))
        {
            return (RedsysLanguage)numeric;
        }

        return RedsysLanguage.Spanish;
    }

    /// <summary>
    /// Maps a standard <see cref="CultureInfo"/> to the corresponding <see cref="RedsysLanguage"/>.
    /// </summary>
    public static RedsysLanguage FromCultureInfo(CultureInfo? culture)
    {
        if (culture is null)
        {
            return RedsysLanguage.Spanish;
        }

        string twoLetter = culture.TwoLetterISOLanguageName.ToLowerInvariant();
        return twoLetter switch
        {
            "es" => RedsysLanguage.Spanish,
            "en" => RedsysLanguage.English,
            "ca" => RedsysLanguage.Catalan,
            "fr" => RedsysLanguage.French,
            "de" => RedsysLanguage.German,
            "nl" => RedsysLanguage.Dutch,
            "it" => RedsysLanguage.Italian,
            "sv" => RedsysLanguage.Swedish,
            "pt" => RedsysLanguage.Portuguese,
            "pl" => RedsysLanguage.Polish,
            "gl" => RedsysLanguage.Galician,
            "eu" => RedsysLanguage.Basque,
            _ => RedsysLanguage.Spanish
        };
    }
}
