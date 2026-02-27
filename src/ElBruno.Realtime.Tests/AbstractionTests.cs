namespace ElBruno.Realtime.Tests;

/// <summary>Tests for the abstraction types (options, events, segments).</summary>
public class AbstractionTests
{
    [Fact]
    public void TextToSpeechOptions_Clone_CreatesIndependentCopy()
    {
        var original = new TextToSpeechOptions
        {
            ModelId = "qwen-tts",
            VoiceId = "ryan",
            Language = "en",
            SampleRate = 24000,
            Speed = 1.2f,
            AdditionalProperties = new Dictionary<string, object?> { ["key"] = "value" },
        };

        var clone = original.Clone();

        Assert.Equal(original.ModelId, clone.ModelId);
        Assert.Equal(original.VoiceId, clone.VoiceId);
        Assert.Equal(original.Language, clone.Language);
        Assert.Equal(original.SampleRate, clone.SampleRate);
        Assert.Equal(original.Speed, clone.Speed);
        Assert.NotSame(original.AdditionalProperties, clone.AdditionalProperties);
    }

    [Fact]
    public void VadOptions_Clone_CreatesIndependentCopy()
    {
        var original = new VadOptions
        {
            SpeechThreshold = 0.7f,
            MinSpeechDurationMs = 500,
            MinSilenceDurationMs = 400,
            SampleRate = 8000,
            Channels = 2,
        };

        var clone = original.Clone();

        Assert.Equal(0.7f, clone.SpeechThreshold);
        Assert.Equal(500, clone.MinSpeechDurationMs);
        Assert.Equal(400, clone.MinSilenceDurationMs);
        Assert.Equal(8000, clone.SampleRate);
        Assert.Equal(2, clone.Channels);
    }

    [Fact]
    public void SpeechSegment_Duration_CalculatedCorrectly()
    {
        var segment = new SpeechSegment
        {
            StartTime = TimeSpan.FromSeconds(1.5),
            EndTime = TimeSpan.FromSeconds(3.2),
        };

        Assert.Equal(TimeSpan.FromSeconds(1.7), segment.Duration);
    }

    [Fact]
    public void ConversationEvent_Timestamp_SetByDefault()
    {
        var evt = new ConversationEvent
        {
            Kind = ConversationEventKind.SpeechDetected,
        };

        Assert.True(evt.Timestamp > DateTimeOffset.MinValue);
    }

    [Fact]
    public void ConversationOptions_DefaultValues()
    {
        var options = new ConversationOptions();

        Assert.True(options.EnableBargeIn);
        Assert.Equal(20, options.MaxConversationHistory);
        Assert.True(options.EnableAudioResponse);
        Assert.Null(options.SystemPrompt);
    }

    [Fact]
    public void TextToSpeechResponse_Dispose_CleansUpStream()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var response = new TextToSpeechResponse { AudioStream = stream };

        response.Dispose();

        Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
    }

    [Fact]
    public void ConversationTurn_Dispose_CleansUpStream()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var turn = new ConversationTurn { ResponseAudio = stream };

        turn.Dispose();

        Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
    }

    [Theory]
    [InlineData(ConversationEventKind.SpeechDetected)]
    [InlineData(ConversationEventKind.TranscriptionPartial)]
    [InlineData(ConversationEventKind.TranscriptionComplete)]
    [InlineData(ConversationEventKind.ResponseStarted)]
    [InlineData(ConversationEventKind.ResponseTextChunk)]
    [InlineData(ConversationEventKind.ResponseAudioChunk)]
    [InlineData(ConversationEventKind.ResponseComplete)]
    [InlineData(ConversationEventKind.Interrupted)]
    [InlineData(ConversationEventKind.Error)]
    public void ConversationEventKind_AllValuesValid(ConversationEventKind kind)
    {
        var evt = new ConversationEvent { Kind = kind };
        Assert.Equal(kind, evt.Kind);
    }

    [Theory]
    [InlineData(TextToSpeechUpdateKind.SessionOpen)]
    [InlineData(TextToSpeechUpdateKind.AudioChunk)]
    [InlineData(TextToSpeechUpdateKind.SessionClose)]
    [InlineData(TextToSpeechUpdateKind.Error)]
    public void TextToSpeechUpdateKind_AllValuesValid(TextToSpeechUpdateKind kind)
    {
        var update = new TextToSpeechResponseUpdate { Kind = kind };
        Assert.Equal(kind, update.Kind);
    }
}
