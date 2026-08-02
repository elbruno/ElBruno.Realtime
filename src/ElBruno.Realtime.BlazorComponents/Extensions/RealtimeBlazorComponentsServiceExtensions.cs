using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.Realtime.BlazorComponents;

/// <summary>
/// Extension methods for registering Blazor real-time conversation components.
/// </summary>
public static class RealtimeBlazorComponentsServiceExtensions
{
    /// <summary>
    /// Registers services required by the <c>ElBruno.Realtime.BlazorComponents</c> Razor
    /// components. Call this after <c>AddPersonaPlexRealtime()</c> in <c>Program.cs</c>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddPersonaPlexRealtime(opts => { ... })
    ///                 .UseWhisperStt("whisper-tiny.en")
    ///                 .UseSileroVad();
    /// builder.Services.AddRealtimeBlazorComponents();
    /// </code>
    /// </example>
    public static IServiceCollection AddRealtimeBlazorComponents(this IServiceCollection services)
    {
        // Components are stateless Razor components — no additional DI registrations
        // are required beyond what AddPersonaPlexRealtime() already provides.
        // This method serves as the conventional entry point so consuming apps have a
        // clear call to make, and it can be extended in future releases.
        return services;
    }
}
