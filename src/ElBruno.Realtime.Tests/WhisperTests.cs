using ElBruno.Realtime.Whisper;

namespace ElBruno.Realtime.Tests;

/// <summary>Tests for WhisperModelManager (non-download tests).</summary>
public class WhisperModelManagerTests
{
    [Fact]
    public void SupportedModels_ContainsExpectedModels()
    {
        var models = WhisperModelManager.SupportedModels;

        Assert.Contains("whisper-tiny.en", models);
        Assert.Contains("whisper-base.en", models);
        Assert.Contains("whisper-tiny", models);
        Assert.Contains("whisper-base", models);
        Assert.Contains("whisper-small", models);
        Assert.Contains("whisper-medium", models);
        Assert.Contains("whisper-large-v3", models);
    }

    [Fact]
    public async Task EnsureModelAsync_ThrowsForUnknownModel()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => WhisperModelManager.EnsureModelAsync("whisper-nonexistent"));
    }
}

/// <summary>Tests for WhisperSpeechToTextClient (non-download tests).</summary>
public class WhisperSpeechToTextClientTests
{
    [Fact]
    public void GetService_ReturnsSelfForOwnType()
    {
        using var client = new WhisperSpeechToTextClient();
        var service = client.GetService(typeof(WhisperSpeechToTextClient));
        Assert.Same(client, service);
    }

    [Fact]
    public void GetService_ReturnsNullForUnknownType()
    {
        using var client = new WhisperSpeechToTextClient();
        var service = client.GetService(typeof(string));
        Assert.Null(service);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var client = new WhisperSpeechToTextClient();
        client.Dispose();
        client.Dispose(); // Should not throw
    }

    [Fact]
    public async Task GetTextAsync_ThrowsWhenDisposed()
    {
        var client = new WhisperSpeechToTextClient();
        client.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => client.GetTextAsync(new MemoryStream()));
    }
}
