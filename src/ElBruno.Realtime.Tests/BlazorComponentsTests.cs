using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ElBruno.Realtime.BlazorComponents;

namespace ElBruno.Realtime.Tests;

/// <summary>Tests for the ElBruno.Realtime.BlazorComponents package.</summary>
public class BlazorComponentsTests
{
    // -----------------------------------------------------------------------
    // DI Registration
    // -----------------------------------------------------------------------

    [Fact]
    public void AddRealtimeBlazorComponents_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();
        var result = services.AddRealtimeBlazorComponents();
        Assert.NotNull(result);
    }

    [Fact]
    public void AddRealtimeBlazorComponents_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddRealtimeBlazorComponents();
        Assert.Same(services, result);
    }

    // -----------------------------------------------------------------------
    // Enum values
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(AudioSource.Microphone)]
    [InlineData(AudioSource.Speaker)]
    public void AudioSource_AllValuesValid(AudioSource source)
    {
        Assert.True(Enum.IsDefined(source));
    }

    [Fact]
    public void AudioSource_HasExactlyTwoValues()
    {
        Assert.Equal(2, Enum.GetValues<AudioSource>().Length);
    }

    [Theory]
    [InlineData(ControlSize.Small)]
    [InlineData(ControlSize.Medium)]
    [InlineData(ControlSize.Large)]
    public void ControlSize_AllValuesValid(ControlSize size)
    {
        Assert.True(Enum.IsDefined(size));
    }

    [Fact]
    public void ControlSize_HasExactlyThreeValues()
    {
        Assert.Equal(3, Enum.GetValues<ControlSize>().Length);
    }

    // -----------------------------------------------------------------------
    // Component type discovery (assembly scan)
    // -----------------------------------------------------------------------

    private static readonly Assembly ComponentsAssembly =
        typeof(RealtimeBlazorComponentsServiceExtensions).Assembly;

    [Theory]
    [InlineData("ConversationTimeline")]
    [InlineData("RealtimeSessionPanel")]
    [InlineData("AudioLevelMeter")]
    [InlineData("SessionControls")]
    [InlineData("LatencyMonitor")]
    public void Component_TypeExistsInAssembly(string typeName)
    {
        var type = ComponentsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == typeName);

        Assert.NotNull(type);
    }

    // -----------------------------------------------------------------------
    // ConversationTimeline parameters
    // -----------------------------------------------------------------------

    [Fact]
    public void ConversationTimeline_HasMaxTurnsParameter()
    {
        AssertParameterExists("ConversationTimeline", "MaxTurns");
    }

    [Fact]
    public void ConversationTimeline_HasShowAudioPlaybackParameter()
    {
        AssertParameterExists("ConversationTimeline", "ShowAudioPlayback");
    }

    [Fact]
    public void ConversationTimeline_HasTurnsParameter()
    {
        AssertParameterExists("ConversationTimeline", "Turns");
    }

    // -----------------------------------------------------------------------
    // RealtimeSessionPanel parameters
    // -----------------------------------------------------------------------

    [Fact]
    public void RealtimeSessionPanel_HasIsRunningParameter()
    {
        AssertParameterExists("RealtimeSessionPanel", "IsRunning");
    }

    [Fact]
    public void RealtimeSessionPanel_HasOnStartedParameter()
    {
        AssertParameterExists("RealtimeSessionPanel", "OnStarted");
    }

    [Fact]
    public void RealtimeSessionPanel_HasOnStoppedParameter()
    {
        AssertParameterExists("RealtimeSessionPanel", "OnStopped");
    }

    // -----------------------------------------------------------------------
    // AudioLevelMeter parameters
    // -----------------------------------------------------------------------

    [Fact]
    public void AudioLevelMeter_HasSourceParameter()
    {
        AssertParameterExists("AudioLevelMeter", "Source");
    }

    [Fact]
    public void AudioLevelMeter_HasHeightParameter()
    {
        AssertParameterExists("AudioLevelMeter", "Height");
    }

    [Fact]
    public void AudioLevelMeter_HasColorParameter()
    {
        AssertParameterExists("AudioLevelMeter", "Color");
    }

    [Fact]
    public void AudioLevelMeter_HasLevelParameter()
    {
        AssertParameterExists("AudioLevelMeter", "Level");
    }

    // -----------------------------------------------------------------------
    // SessionControls parameters
    // -----------------------------------------------------------------------

    [Fact]
    public void SessionControls_HasSizeParameter()
    {
        AssertParameterExists("SessionControls", "Size");
    }

    [Fact]
    public void SessionControls_HasIsRunningParameter()
    {
        AssertParameterExists("SessionControls", "IsRunning");
    }

    // -----------------------------------------------------------------------
    // LatencyMonitor parameters
    // -----------------------------------------------------------------------

    [Fact]
    public void LatencyMonitor_HasSttLatencyMsParameter()
    {
        AssertParameterExists("LatencyMonitor", "SttLatencyMs");
    }

    [Fact]
    public void LatencyMonitor_HasLlmLatencyMsParameter()
    {
        AssertParameterExists("LatencyMonitor", "LlmLatencyMs");
    }

    [Fact]
    public void LatencyMonitor_HasTtsLatencyMsParameter()
    {
        AssertParameterExists("LatencyMonitor", "TtsLatencyMs");
    }

    [Fact]
    public void LatencyMonitor_HasShowAverageParameter()
    {
        AssertParameterExists("LatencyMonitor", "ShowAverage");
    }

    // -----------------------------------------------------------------------
    // Helper
    // -----------------------------------------------------------------------

    private static void AssertParameterExists(string typeName, string propertyName)
    {
        var type = ComponentsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == typeName);

        Assert.NotNull(type);

        var prop = type!.GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(prop);

        var attr = prop!.GetCustomAttribute<Microsoft.AspNetCore.Components.ParameterAttribute>();
        Assert.NotNull(attr);
    }
}
