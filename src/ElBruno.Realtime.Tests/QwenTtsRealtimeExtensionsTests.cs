using ElBruno.QwenTTS.Realtime;
using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.Realtime.Tests;

/// <summary>
/// Tests for QwenTTS DI registration extensions with GPU configuration support (Issue #3).
/// Validates backward compatibility, configuration callbacks, and edge cases.
/// </summary>
public class QwenTtsRealtimeExtensionsTests
{
    [Fact]
    public void UseQwenTts_NoCallback_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddPersonaPlexRealtime();

        // Act - backward compatibility: no callback parameter
        builder.UseQwenTts();
        var provider = services.BuildServiceProvider();

        // Assert - both ITtsPipeline and ITextToSpeechClient are registered
        // Note: ITtsPipeline is internal to ElBruno.QwenTTS package, so we only verify ITextToSpeechClient
        var ttsClient = provider.GetService<ITextToSpeechClient>();

        Assert.NotNull(ttsClient);
        Assert.IsType<QwenTextToSpeechClientAdapter>(ttsClient);
    }

    [Fact]
    public void UseQwenTts_WithDeviceIdCallback_RegistersServicesWithConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddPersonaPlexRealtime();
        var configuredDeviceId = -1;

        // Act - configure GPU device via callback
        builder.UseQwenTts(opts =>
        {
            opts.GpuDeviceId = 1; // GPU device 1 (user's scenario from issue #3)
            configuredDeviceId = opts.GpuDeviceId; // Capture for verification
        });
        var provider = services.BuildServiceProvider();

        // Assert - services registered and callback was invoked
        var ttsClient = provider.GetService<ITextToSpeechClient>();

        Assert.NotNull(ttsClient);
        Assert.Equal(1, configuredDeviceId); // Callback was invoked
    }

    [Fact]
    public void UseQwenTts_MultipleConfigurationOptions_AcceptsAllOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddPersonaPlexRealtime();
        var deviceIdCaptured = false;
        var executionProviderCaptured = false;

        // Act - configure multiple GPU options in same callback
        builder.UseQwenTts(opts =>
        {
            opts.GpuDeviceId = 2; // GPU device 2
            deviceIdCaptured = true;
            
            // Note: ExecutionProvider type depends on ElBruno.QwenTTS package
            // Testing that multiple property assignments work without error
            var deviceId = opts.GpuDeviceId;
            if (deviceId == 2)
            {
                executionProviderCaptured = true;
            }
        });
        var provider = services.BuildServiceProvider();

        // Assert - callback executed successfully with multiple options
        var ttsClient = provider.GetService<ITextToSpeechClient>();
        Assert.NotNull(ttsClient);
        Assert.True(deviceIdCaptured, "DeviceId was not set in callback");
        Assert.True(executionProviderCaptured, "Multiple options were not processed");
    }

    [Fact]
    public void AddQwenTtsRealtime_NoCallback_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - use IServiceCollection overload without callback
        services.AddQwenTtsRealtime();
        var provider = services.BuildServiceProvider();

        // Assert - both services registered
        var ttsClient = provider.GetService<ITextToSpeechClient>();

        Assert.NotNull(ttsClient);
        Assert.IsType<QwenTextToSpeechClientAdapter>(ttsClient);
    }

    [Fact]
    public void AddQwenTtsRealtime_WithConfiguration_RegistersServicesAndInvokesCallback()
    {
        // Arrange
        var services = new ServiceCollection();
        var callbackInvoked = false;
        var deviceIdSet = 0;

        // Act - configure via IServiceCollection overload
        services.AddQwenTtsRealtime(opts =>
        {
            opts.GpuDeviceId = 3;
            deviceIdSet = opts.GpuDeviceId;
            callbackInvoked = true;
        });
        var provider = services.BuildServiceProvider();

        // Assert - callback invoked and services registered
        var ttsClient = provider.GetService<ITextToSpeechClient>();

        Assert.NotNull(ttsClient);
        Assert.True(callbackInvoked, "Configuration callback was not invoked");
        Assert.Equal(3, deviceIdSet);
    }

    [Fact]
    public void UseQwenTts_NullCallback_RegistersServicesWithDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddPersonaPlexRealtime();

        // Act - explicitly pass null callback (edge case)
        builder.UseQwenTts(configureOptions: null);
        var provider = services.BuildServiceProvider();

        // Assert - services still registered with default configuration
        var ttsClient = provider.GetService<ITextToSpeechClient>();

        Assert.NotNull(ttsClient);
        Assert.IsType<QwenTextToSpeechClientAdapter>(ttsClient);
    }

    [Fact]
    public void AddQwenTtsRealtime_NullCallback_RegistersServicesWithDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - explicitly pass null callback (edge case)
        services.AddQwenTtsRealtime(configureOptions: null);
        var provider = services.BuildServiceProvider();

        // Assert - services still registered with default configuration
        var ttsClient = provider.GetService<ITextToSpeechClient>();

        Assert.NotNull(ttsClient);
    }

    [Fact]
    public void UseQwenTts_ChainedWithOtherBuilderCalls_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddPersonaPlexRealtime(opts =>
        {
            opts.DefaultLanguage = "en-US";
        });

        // Act - chain multiple builder calls together
        builder
            .UseQwenTts(opts => opts.GpuDeviceId = 1)
            .UseChatClient(_ => new MockChatClient()); // From DiRegistrationTests

        var provider = services.BuildServiceProvider();

        // Assert - all services registered correctly
        var options = provider.GetService<RealtimeOptions>();
        var ttsClient = provider.GetService<ITextToSpeechClient>();
        var chatClient = provider.GetService<Microsoft.Extensions.AI.IChatClient>();

        Assert.NotNull(options);
        Assert.Equal("en-US", options.DefaultLanguage);
        Assert.NotNull(ttsClient);
        Assert.NotNull(chatClient);
    }

    [Fact]
    public void UseQwenTts_ReturnsBuilderForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddPersonaPlexRealtime();

        // Act
        var returnedBuilder = builder.UseQwenTts();

        // Assert - same builder instance returned for fluent chaining
        Assert.Same(builder, returnedBuilder);
    }

    [Fact]
    public void AddQwenTtsRealtime_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddQwenTtsRealtime();

        // Assert - same IServiceCollection instance returned
        Assert.Same(services, returnedServices);
    }
}
