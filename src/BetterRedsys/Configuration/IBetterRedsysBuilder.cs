using Microsoft.Extensions.DependencyInjection;

namespace BetterRedsys.Configuration;

/// <summary>
/// Builder interface for configuring and extending BetterRedsys components in the dependency injection container.
/// </summary>
public interface IBetterRedsysBuilder
{
    /// <summary>
    /// The application service collection.
    /// </summary>
    IServiceCollection Services { get; }
}

/// <summary>
/// Default implementation of <see cref="IBetterRedsysBuilder"/>.
/// </summary>
internal sealed class BetterRedsysBuilder(IServiceCollection services) : IBetterRedsysBuilder
{
    public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
}
