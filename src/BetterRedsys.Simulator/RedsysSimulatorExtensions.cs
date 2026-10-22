using BetterRedsys.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BetterRedsys.Simulator;

/// <summary>
/// Extension methods for configuring and enabling the Redsys Simulator in ASP.NET Core applications.
/// </summary>
public static class RedsysSimulatorExtensions
{
    /// <summary>
    /// Registers the BetterRedsys in-memory simulator services in the dependency injection container,
    /// configuring local routes and redirecting Redsys payment gateway URLs to the in-memory simulator.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration callback for simulator options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBetterRedsysSimulator(
        this IServiceCollection services,
        Action<RedsysSimulatorOptions>? configure = null)
    {
        var options = new RedsysSimulatorOptions();
        configure?.Invoke(options);

        services.Configure<RedsysSimulatorOptions>(opt =>
        {
            opt.Path = options.Path;
            opt.RestPath = options.RestPath;
            opt.AutoConfigureGatewayUrls = options.AutoConfigureGatewayUrls;
        });

        if (options.AutoConfigureGatewayUrls)
        {
            services.PostConfigure<RedsysOptions>(redsysOpt =>
            {
                redsysOpt.CustomGatewayUrl = options.Path;
                redsysOpt.CustomRestGatewayUrl = options.RestPath;
            });
        }

        services.AddHttpClient("BetterRedsysSimulator");

        return services;
    }

    /// <summary>
    /// Enables the interactive Redsys simulator middleware to intercept and simulate payment flows locally without ngrok or real bank connectivity.
    /// Recommended to run conditionally under <c>if (app.Environment.IsDevelopment())</c>.
    /// </summary>
    /// <param name="app">The ASP.NET Core application pipeline.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> instance for chaining.</returns>
    public static IApplicationBuilder UseRedsysSimulator(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RedsysSimulatorMiddleware>();
    }
}
