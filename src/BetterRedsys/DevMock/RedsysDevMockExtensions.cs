using Microsoft.AspNetCore.Builder;

namespace BetterRedsys.DevMock;

/// <summary>
/// Extension methods for enabling the Redsys DevMock simulator in the ASP.NET Core middleware pipeline.
/// </summary>
public static class RedsysDevMockExtensions
{
    /// <summary>
    /// Enables the interactive Redsys DevMock simulator middleware to intercept and simulate payment flows locally without ngrok or real bank connectivity.
    /// Recommended to run conditionally under <c>if (app.Environment.IsDevelopment())</c>.
    /// </summary>
    /// <param name="app">The ASP.NET Core application pipeline.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> instance for chaining.</returns>
    public static IApplicationBuilder UseRedsysDevMock(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RedsysDevMockMiddleware>();
    }
}
