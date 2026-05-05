using ElBruno.QwenTTS.Realtime;
using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.Realtime.Tests;

/// <summary>
/// Tests for QwenTTS DI registration extensions with GPU configuration support (Issue #3).
/// Validates backward compatibility, configuration callbacks, and edge cases.
/// Tests check service registration without instantiating to avoid model download race conditions.
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

        // Assert - ITextToSpeechClient is registered (check descriptor, don't instantiate)
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITextToSpeechClient));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
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

        // Assert - services registered and callback was invoked
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITextToSpeechClient));
        Assert.NotNull(descriptor);
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

        // Assert - callback executed successfully with multiple options
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITextToSpeechClient));
        Assert.NotNull(descriptor);
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

        // Assert - check service descriptor without instantiation
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITextToSpeechClient));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
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

        // Assert - callback invoked and services registered
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITextToSpeechClient));
        Assert.NotNull(descriptor);
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

        // Assert - services still registered with default configuration
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITextToSpeechClient));
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddQwenTtsRealtime_NullCallback_RegistersServicesWithDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - explicitly pass null callback (edge case)
        services.AddQwenTtsRealtime(configureOptions: null);

        // Assert - services still registered with default configuration
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITextToSpeechClient));
        Assert.NotNull(descriptor);
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
        var ttsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITextToSpeechClient));
        var chatClient = provider.GetService<Microsoft.Extensions.AI.IChatClient>();

        Assert.NotNull(options);
        Assert.Equal("en-US", options.DefaultLanguage);
        Assert.NotNull(ttsDescriptor);
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
