using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ElBruno.Realtime.Pipeline;

namespace ElBruno.Realtime.Tests;

/// <summary>Tests for DI registration and interface contracts.</summary>
public class DiRegistrationTests
{
    [Fact]
    public void AddPersonaPlexRealtime_RegistersOptions()
    {
        var services = new ServiceCollection();
        services.AddPersonaPlexRealtime(opts =>
        {
            opts.DefaultLanguage = "en-US";
            opts.DefaultSystemPrompt = "You are a test assistant.";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<RealtimeOptions>();

        Assert.Equal("en-US", options.DefaultLanguage);
        Assert.Equal("You are a test assistant.", options.DefaultSystemPrompt);
    }

    [Fact]
    public void AddPersonaPlexRealtime_ReturnsBuilder()
    {
        var services = new ServiceCollection();
        var builder = services.AddPersonaPlexRealtime();

        Assert.NotNull(builder);
        Assert.Same(services, builder.Services);
        Assert.NotNull(builder.Options);
    }

    [Fact]
    public void Builder_UseChatClient_RegistersIChatClient()
    {
        var services = new ServiceCollection();
        var mockChat = new MockChatClient();

        services.AddPersonaPlexRealtime()
            .UseChatClient(_ => mockChat);

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IChatClient>();

        Assert.Same(mockChat, resolved);
    }

    [Fact]
    public void RealtimeOptions_DefaultValues()
    {
        var options = new RealtimeOptions();

        Assert.Equal("en-US", options.DefaultLanguage);
        Assert.Null(options.DefaultSystemPrompt);
        Assert.Equal("whisper-tiny.en", options.SpeechToText.ModelId);
        Assert.Equal(0.5f, options.VoiceActivityDetection.SpeechThreshold);
        Assert.Equal(300, options.VoiceActivityDetection.MinSilenceDurationMs);
    }
}

/// <summary>A minimal mock IChatClient for testing.</summary>
internal class MockChatClient : IChatClient
{
    public void Dispose() { }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "test reply")));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return EmptyAsync();
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmptyAsync()
    {
        await Task.CompletedTask;
        yield break;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}
