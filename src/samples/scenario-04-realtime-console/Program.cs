using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ElBruno.Realtime;
using ElBruno.Realtime.Whisper;
using ElBruno.QwenTTS.Pipeline;
using NAudio.Wave;
using Scenario04RealtimeConsole;

// ──────────────────────────────────────────────────────────────────
// Scenario 04: Real-Time Microphone Conversation
//
// Captures audio from the default microphone, transcribes with
// Whisper, sends to Ollama LLM, and speaks the response.
// Continuous conversation loop until Ctrl+C.
//
// Pipeline:  Microphone → Whisper STT → Ollama LLM → QwenTTS → Speakers
//
// Prerequisites:
//   - Ollama running locally with phi4-mini:
//     ollama pull phi4-mini && ollama serve
//   - A working microphone
// ──────────────────────────────────────────────────────────────────

Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  PersonaPlex Realtime Console - Scenario 04     ║");
Console.WriteLine("║  Real-time Microphone Conversation              ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine();

// ── 1. Check for available microphones ──────────────────────────
var deviceCount = WaveInEvent.DeviceCount;
if (deviceCount == 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Log("❌ No microphone found. Please connect a microphone and try again.");
    Console.ResetColor();
    return;
}

Log("🎙️  Available microphones:");
for (var i = 0; i < deviceCount; i++)
{
    var caps = WaveInEvent.GetCapabilities(i);
    Console.WriteLine($"   [{i}] {caps.ProductName}");
}

var deviceNumber = 0;
Console.WriteLine($"   Using device [{deviceNumber}]");
Console.WriteLine();

// ── 2. Configure services ───────────────────────────────────────
var services = new ServiceCollection();

services.AddPersonaPlexRealtime(opts =>
{
    opts.DefaultSystemPrompt = "You are a helpful assistant. Keep responses brief (1-2 sentences).";
    opts.DefaultLanguage = "en-US";
})
.UseWhisperStt("whisper-tiny.en");  // 75MB model, auto-downloads on first use

// Register TTS pipeline and adapter for ITextToSpeechClient
services.AddQwenTts();
services.AddSingleton<ITextToSpeechClient, QwenTextToSpeechClientAdapter>();

// Register Ollama as the LLM (assumes Ollama is running locally)
services.AddChatClient(new OllamaChatClient(
    new Uri("http://localhost:11434"), "phi4-mini"));

var provider = services.BuildServiceProvider();

// ── 3. Get the conversation client ──────────────────────────────
var conversation = provider.GetRequiredService<IRealtimeConversationClient>();

Log("✅ Pipeline initialized");
Console.WriteLine("   STT:  Whisper tiny.en (auto-download on first use)");
Console.WriteLine("   LLM:  Ollama phi4-mini (localhost:11434)");
Console.WriteLine("   TTS:  QwenTTS");
Console.WriteLine();
Log("Press Ctrl+C to exit.");
Console.WriteLine();

// ── 4. Conversation loop ────────────────────────────────────────
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine();
    Log("👋 Exiting...");
};

var options = new ConversationOptions
{
    SystemPrompt = "You are a helpful, friendly assistant. Keep responses concise.",
    EnableAudioResponse = true,
};

const int sampleRate = 16000;
const int bitsPerSample = 16;
const int channels = 1;
const int minimumAudioBytes = sampleRate * (bitsPerSample / 8) * 1; // ~1 second minimum

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        Log("🎤 Listening... (speak, then pause for 1.5s to process)");

        byte[] audioData;
        try
        {
            audioData = await RecordUntilSilenceAsync(deviceNumber, sampleRate, bitsPerSample, channels, cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }

        if (audioData.Length < minimumAudioBytes)
        {
            Log("(no speech detected, listening again...)");
            Console.WriteLine();
            continue;
        }

        try
        {
            var wavData = CreateWavData(audioData, sampleRate, bitsPerSample, channels);
            using var audioStream = new MemoryStream(wavData);
            
            Log("🔄 Transcribing...");
            var turn = await conversation.ProcessTurnAsync(audioStream, options, cts.Token);

            Log($"📝 You said: {turn.UserText}");
            Log($"🤖 AI replied: {turn.ResponseText}");
            
            if (turn.ResponseAudio is not null)
            {
                Log("🔊 Playing audio response...");
                await PlayAudioAsync(turn.ResponseAudio, cts.Token);
            }
            
            Log($"⏱️  Total: {turn.ProcessingTime.TotalSeconds:F1}s");
            Console.WriteLine();
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Connection refused"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log("❌ Cannot connect to Ollama. Make sure it's running:");
            Console.WriteLine("   ollama serve");
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
catch (OperationCanceledException)
{
    // Normal exit via Ctrl+C
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Log($"❌ Error: {ex.Message}");
    Console.ResetColor();
}

Console.WriteLine();
Log("Done.");

// ═════════════════════════════════════════════════════════════════
// Helper methods
// ═════════════════════════════════════════════════════════════════

/// <summary>
/// Logs a message with a timestamp prefix in [HH:mm:ss] format.
/// </summary>
static void Log(string message)
{
    var timestamp = DateTime.Now.ToString("[HH:mm:ss]");
    Console.WriteLine($"{timestamp} {message}");
}

/// <summary>
/// Records audio from the microphone until silence is detected after speech,
/// or the maximum recording time (30s) is reached.
/// </summary>
static async Task<byte[]> RecordUntilSilenceAsync(
    int deviceNumber, int sampleRate, int bitsPerSample, int channels,
    CancellationToken cancellationToken)
{
    using var memoryStream = new MemoryStream();
    var tcs = new TaskCompletionSource<byte[]>();
    using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

    var waveIn = new WaveInEvent
    {
        DeviceNumber = deviceNumber,
        WaveFormat = new WaveFormat(sampleRate, bitsPerSample, channels),
        BufferMilliseconds = 100,
    };

    const float silenceThreshold = 500f;       // RMS threshold for silence
    const float speechThreshold = 1000f;       // RMS threshold to consider speech started
    const double silenceDurationSec = 1.5;     // Seconds of silence after speech to stop
    const double maxRecordingSec = 30.0;       // Safety limit

    var speechDetected = false;
    var silenceStart = DateTime.MinValue;
    var recordingStart = DateTime.UtcNow;

    waveIn.DataAvailable += (_, e) =>
    {
        if (tcs.Task.IsCompleted)
            return;

        // Calculate RMS amplitude
        var rms = CalculateRms(e.Buffer, e.BytesRecorded);
        var elapsed = (DateTime.UtcNow - recordingStart).TotalSeconds;

        if (elapsed >= maxRecordingSec)
        {
            memoryStream.Write(e.Buffer, 0, e.BytesRecorded);
            tcs.TrySetResult(memoryStream.ToArray());
            return;
        }

        if (!speechDetected)
        {
            if (rms >= speechThreshold)
            {
                speechDetected = true;
                silenceStart = DateTime.MinValue;
            }
        }
        else
        {
            if (rms < silenceThreshold)
            {
                if (silenceStart == DateTime.MinValue)
                    silenceStart = DateTime.UtcNow;
                else if ((DateTime.UtcNow - silenceStart).TotalSeconds >= silenceDurationSec)
                {
                    tcs.TrySetResult(memoryStream.ToArray());
                    return;
                }
            }
            else
            {
                silenceStart = DateTime.MinValue;
            }
        }

        memoryStream.Write(e.Buffer, 0, e.BytesRecorded);
    };

    waveIn.RecordingStopped += (_, e) =>
    {
        if (e.Exception is not null)
            tcs.TrySetException(e.Exception);
        else
            tcs.TrySetResult(memoryStream.ToArray());
    };

    try
    {
        waveIn.StartRecording();
        var result = await tcs.Task;
        waveIn.StopRecording();
        return result;
    }
    catch (OperationCanceledException)
    {
        waveIn.StopRecording();
        throw;
    }
    catch (Exception)
    {
        waveIn.StopRecording();
        throw;
    }
}

/// <summary>
/// Calculates the RMS (Root Mean Square) amplitude of 16-bit PCM audio.
/// </summary>
static float CalculateRms(byte[] buffer, int bytesRecorded)
{
    var sampleCount = bytesRecorded / 2; // 16-bit samples
    if (sampleCount == 0) return 0f;

    double sumSquares = 0;
    for (var i = 0; i < bytesRecorded - 1; i += 2)
    {
        var sample = (short)(buffer[i] | (buffer[i + 1] << 8));
        sumSquares += sample * (double)sample;
    }

    return (float)Math.Sqrt(sumSquares / sampleCount);
}

/// <summary>
/// Wraps raw PCM data with a WAV header (RIFF format).
/// The pipeline expects WAV format: 16kHz, 16-bit, mono.
/// </summary>
static byte[] CreateWavData(byte[] pcmData, int sampleRate, int bitsPerSample, int channels)
{
    var blockAlign = channels * (bitsPerSample / 8);
    var byteRate = sampleRate * blockAlign;

    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);

    // RIFF header
    writer.Write("RIFF"u8);
    writer.Write(36 + pcmData.Length);      // File size - 8
    writer.Write("WAVE"u8);

    // fmt sub-chunk
    writer.Write("fmt "u8);
    writer.Write(16);                       // Sub-chunk size (PCM = 16)
    writer.Write((short)1);                 // Audio format (PCM = 1)
    writer.Write((short)channels);
    writer.Write(sampleRate);
    writer.Write(byteRate);
    writer.Write((short)blockAlign);
    writer.Write((short)bitsPerSample);

    // data sub-chunk
    writer.Write("data"u8);
    writer.Write(pcmData.Length);
    writer.Write(pcmData);

    return stream.ToArray();
}

/// <summary>
/// Plays audio through the default output device using NAudio.
/// Blocks until playback completes.
/// </summary>
static async Task PlayAudioAsync(Stream audioStream, CancellationToken cancellationToken)
{
    audioStream.Position = 0;
    var audioBytes = new byte[audioStream.Length];
    await audioStream.ReadAsync(audioBytes, cancellationToken);
    
    using var ms = new MemoryStream(audioBytes);
    using var reader = new WaveFileReader(ms);
    using var waveOut = new WaveOutEvent();
    
    waveOut.Init(reader);
    waveOut.Play();
    
    while (waveOut.PlaybackState == PlaybackState.Playing && !cancellationToken.IsCancellationRequested)
    {
        await Task.Delay(100, cancellationToken);
    }
}
