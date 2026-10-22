using System.Reflection;

namespace BetterRedsys.Simulator.UI;

/// <summary>
/// Static loader and renderer for embedded UI assets (HTML, CSS, JS) of the BetterRedsys simulator.
/// Completely decoupled from C# middleware code and 100% Native AOT compatible.
/// </summary>
internal static class SimulatorUiResources
{
    private static readonly Assembly Assembly = typeof(SimulatorUiResources).Assembly;

    private static readonly string SimulatorCss = LoadString("BetterRedsys.Simulator.UI.simulator.css");
    private static readonly string SimulatorJs = LoadString("BetterRedsys.Simulator.UI.simulator.js");
    private static readonly string SimulatorHtml = LoadString("BetterRedsys.Simulator.UI.simulator.html")
        .Replace("/* {{CSS}} */", SimulatorCss)
        .Replace("{{CSS}}", SimulatorCss)
        .Replace("/* {{JS}} */", SimulatorJs)
        .Replace("{{JS}}", SimulatorJs);

    private static readonly string LandingHtml = LoadString("BetterRedsys.Simulator.UI.landing.html");

    private static string LoadString(string resourceName)
    {
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded UI resource '{resourceName}' not found in BetterRedsys.Simulator assembly.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Renders the interactive payment simulator HTML with order and transaction parameters.
    /// </summary>
    public static string RenderSimulatorHtml(
        string amount,
        string order,
        string merchantCode,
        string terminal,
        string payMethods,
        string bizumBadge,
        string consumerLang,
        string consumerLangCode,
        string customerMobile,
        string description,
        string merchantUrl,
        string urlOk,
        string urlKo,
        string dispatchUrl,
        string initialDataJson)
    {
        return SimulatorHtml
            .Replace("{{AMOUNT}}", amount)
            .Replace("{{ORDER}}", order)
            .Replace("{{MERCHANT_CODE}}", merchantCode)
            .Replace("{{TERMINAL}}", terminal)
            .Replace("{{PAY_METHODS}}", payMethods)
            .Replace("{{BIZUM_BADGE}}", bizumBadge)
            .Replace("{{CONSUMER_LANG}}", consumerLang)
            .Replace("{{CONSUMER_LANG_CODE}}", consumerLangCode)
            .Replace("{{CUSTOMER_MOBILE}}", customerMobile)
            .Replace("{{DESCRIPTION}}", description)
            .Replace("{{MERCHANT_URL}}", merchantUrl)
            .Replace("{{URL_OK}}", urlOk)
            .Replace("{{URL_KO}}", urlKo)
            .Replace("{{DISPATCH_URL}}", dispatchUrl)
            .Replace("{{INITIAL_DATA_JSON}}", initialDataJson);
    }

    /// <summary>
    /// Renders the simulator direct landing page HTML.
    /// </summary>
    public static string RenderLandingHtml(string endpointPath)
    {
        return LandingHtml.Replace("{{ENDPOINT_PATH}}", endpointPath);
    }
}
