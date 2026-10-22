using System.Diagnostics.CodeAnalysis;
using BetterRedsys.Cryptography;
using BetterRedsys.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterRedsys.Configuration;

/// <summary>
/// Extension methods for registering BetterRedsys services into the ASP.NET Core dependency injection container.
/// </summary>
public static class RedsysServiceCollectionExtensions
{
    /// <summary>
    /// Registers BetterRedsys services and configures options using a delegate action.
    /// Fully compatible with Native AOT compilation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Delegate to configure <see cref="RedsysOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBetterRedsys(
        this IServiceCollection services,
        Action<RedsysOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        RegisterCoreServices(services);

        return services;
    }

    /// <summary>
    /// Registers BetterRedsys services by binding to an <see cref="IConfigurationSection"/> (e.g., from appsettings.json "Redsys").
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">Configuration section containing <see cref="RedsysOptions"/> values.</param>
    /// <returns>The service collection for chaining.</returns>
    [RequiresUnreferencedCode("Dynamic configuration binding via IConfigurationSection may require reflection.")]
    [RequiresDynamicCode("Dynamic configuration binding may require dynamic code generation in Native AOT.")]
    public static IServiceCollection AddBetterRedsys(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        services.Configure<RedsysOptions>(configurationSection);
        RegisterCoreServices(services);

        return services;
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddHttpClient("BetterRedsysDevMock");
        services.TryAddSingleton<IRedsysCryptoService, RedsysCryptoService>();
        services.TryAddScoped<IRedsysClient, RedsysClient>();
    }
}
