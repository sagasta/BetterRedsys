using System.Diagnostics.CodeAnalysis;
using BetterRedsys.Cryptography;
using BetterRedsys.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Configuration;

/// <summary>
/// Extension methods for registering BetterRedsys services into the ASP.NET Core dependency injection container.
/// </summary>
public static class RedsysServiceCollectionExtensions
{
    /// <summary>
    /// Registers BetterRedsys core services and configures options using a delegate action.
    /// Enables eager validation on startup (<c>ValidateOnStart</c>).
    /// Fully compatible with Native AOT compilation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Delegate to configure <see cref="RedsysOptions"/>.</param>
    /// <returns>An <see cref="IBetterRedsysBuilder"/> for chaining modules like HealthChecks or Simulator.</returns>
    public static IBetterRedsysBuilder AddBetterRedsys(
        this IServiceCollection services,
        Action<RedsysOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<RedsysOptions>()
            .Configure(configure)
            .ValidateOnStart();

        RegisterCoreServices(services);

        return new BetterRedsysBuilder(services);
    }

    /// <summary>
    /// Registers BetterRedsys services by automatically binding to configuration using <c>BindConfiguration</c>
    /// (defaults to section "Redsys"). Seamlessly supports User Secrets, Azure Key Vault, Azure App Configuration,
    /// environment variables, and appsettings.json.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="sectionName">Section name in configuration (defaults to "Redsys").</param>
    /// <param name="configure">Optional additional configure callback to override or supplement values in code.</param>
    /// <returns>An <see cref="IBetterRedsysBuilder"/> for chaining modules.</returns>
    [RequiresUnreferencedCode("Dynamic configuration binding via BindConfiguration may require reflection.")]
    [RequiresDynamicCode("Dynamic configuration binding may require dynamic code generation in Native AOT.")]
    public static IBetterRedsysBuilder AddBetterRedsys(
        this IServiceCollection services,
        string sectionName = "Redsys",
        Action<RedsysOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<RedsysOptions>()
            .BindConfiguration(sectionName)
            .ValidateOnStart();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        RegisterCoreServices(services);

        return new BetterRedsysBuilder(services);
    }

    /// <summary>
    /// Registers BetterRedsys services by automatically binding to an <see cref="IConfiguration"/> section (defaults to "Redsys").
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="sectionName">Section name in configuration (defaults to "Redsys").</param>
    /// <param name="configure">Optional additional configure callback to override or supplement values in code.</param>
    /// <returns>An <see cref="IBetterRedsysBuilder"/> for chaining modules.</returns>
    [RequiresUnreferencedCode("Dynamic configuration binding via IConfiguration may require reflection.")]
    [RequiresDynamicCode("Dynamic configuration binding may require dynamic code generation in Native AOT.")]
    public static IBetterRedsysBuilder AddBetterRedsys(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Redsys",
        Action<RedsysOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionName);
        return services.AddBetterRedsys(section, configure);
    }

    /// <summary>
    /// Registers BetterRedsys services by binding to an explicit <see cref="IConfigurationSection"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">Configuration section containing <see cref="RedsysOptions"/> values.</param>
    /// <param name="configure">Optional additional configure callback.</param>
    /// <returns>An <see cref="IBetterRedsysBuilder"/> for chaining modules.</returns>
    [RequiresUnreferencedCode("Dynamic configuration binding via IConfigurationSection may require reflection.")]
    [RequiresDynamicCode("Dynamic configuration binding may require dynamic code generation in Native AOT.")]
    public static IBetterRedsysBuilder AddBetterRedsys(
        this IServiceCollection services,
        IConfigurationSection configurationSection,
        Action<RedsysOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        var optionsBuilder = services.AddOptions<RedsysOptions>()
            .Bind(configurationSection)
            .ValidateOnStart();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        RegisterCoreServices(services);

        return new BetterRedsysBuilder(services);
    }

    /// <summary>
    /// Adds an ASP.NET Core Health Check probe for the Redsys payment gateway to the application.
    /// </summary>
    /// <param name="builder">The <see cref="IBetterRedsysBuilder"/> instance.</param>
    /// <param name="name">Name of the health check (defaults to "redsys").</param>
    /// <param name="failureStatus">Failure status to report when unreachable.</param>
    /// <param name="tags">Tags to associate with the check.</param>
    /// <param name="timeout">Probe timeout.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static IBetterRedsysBuilder AddHealthCheck(
        this IBetterRedsysBuilder builder,
        string name = "redsys",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddHealthChecks()
            .AddRedsys(name, failureStatus, tags, timeout);

        return builder;
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.TryAddSingleton<IValidateOptions<RedsysOptions>, RedsysOptionsValidator>();
        services.AddHttpClient("BetterRedsys");
        services.TryAddSingleton<IRedsysCryptoService, RedsysCryptoService>();
        services.TryAddScoped<IRedsysClient, RedsysClient>();
    }
}
