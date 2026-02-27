using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ElBruno.Realtime.Pipeline;

namespace ElBruno.Realtime;

/// <summary>
/// Extension methods for registering real-time conversation services.
/// </summary>
public static class RealtimeServiceCollectionExtensions
{
    /// <summary>
    /// Adds the PersonaPlex real-time conversation pipeline to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional callback to configure real-time options.</param>
    /// <returns>A <see cref="RealtimeBuilder"/> for chaining provider registrations.</returns>
    public static RealtimeBuilder AddPersonaPlexRealtime(
        this IServiceCollection services,
        Action<RealtimeOptions>? configure = null)
    {
        var options = new RealtimeOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        // Register the pipeline (resolved after all providers are registered)
        services.AddSingleton<IRealtimeConversationClient>(sp =>
        {
            var stt = sp.GetRequiredService<ISpeechToTextClient>();
            var chatClient = sp.GetRequiredService<IChatClient>();
            var opts = sp.GetRequiredService<RealtimeOptions>();
            var vad = sp.GetService<IVoiceActivityDetector>();
            var tts = sp.GetService<ITextToSpeechClient>();

            return new RealtimeConversationPipeline(stt, chatClient, opts, vad, tts);
        });

        return new RealtimeBuilder(services, options);
    }
}

/// <summary>
/// Builder for configuring real-time conversation providers via fluent API.
/// </summary>
public class RealtimeBuilder
{
    /// <summary>Gets the service collection being configured.</summary>
    public IServiceCollection Services { get; }

    /// <summary>Gets the real-time options.</summary>
    public RealtimeOptions Options { get; }

    internal RealtimeBuilder(IServiceCollection services, RealtimeOptions options)
    {
        Services = services;
        Options = options;
    }

    /// <summary>
    /// Registers an <see cref="IChatClient"/> factory for the LLM component.
    /// </summary>
    /// <param name="factory">Factory to resolve the chat client from the service provider.</param>
    /// <returns>The builder for chaining.</returns>
    public RealtimeBuilder UseChatClient(Func<IServiceProvider, IChatClient> factory)
    {
        Services.AddSingleton(factory);
        return this;
    }
}
