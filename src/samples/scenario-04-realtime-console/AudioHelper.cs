using NAudio.Wave;

namespace Scenario04RealtimeConsole;

/// <summary>
/// Audio utilities for microphone recording, WAV encoding, and playback.
/// All audio uses 16kHz, 16-bit, mono PCM — the format required by Whisper.
/// </summary>
public static class AudioHelper
{
    public const int SampleRate = 16000;
    public const int BitsPerSample = 16;
    public const int Channels = 1;

    /// <summary>Minimum audio length (~1 second) to consider as valid speech.</summary>
    public const int MinimumAudioBytes = SampleRate * (BitsPerSample / 8) * 1;

    /// <summary>
    /// Records audio from the microphone until silence is detected after speech,
    /// or the maximum recording time (30s) is reached.
    /// Uses RMS-based voice activity detection:
    ///   - Speech starts when RMS exceeds 1000
    ///   - Silence detected when RMS drops below 500 for 1.5 seconds
    /// </summary>
    public static async Task<byte[]> RecordUntilSilenceAsync(
        int deviceNumber, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        var tcs = new TaskCompletionSource<byte[]>();
        using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        var waveIn = new WaveInEvent
        {
            DeviceNumber = deviceNumber,
            WaveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels),
            BufferMilliseconds = 100,
        };

        const float silenceThreshold = 500f;
        const float speechThreshold = 1000f;
        const double silenceDurationSec = 1.5;
        const double maxRecordingSec = 30.0;

        var speechDetected = false;
        var silenceStart = DateTime.MinValue;
        var recordingStart = DateTime.UtcNow;

        waveIn.DataAvailable += (_, e) =>
        {
            if (tcs.Task.IsCompleted)
                return;

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
        catch
        {
            waveIn.StopRecording();
            throw;
        }
    }

    /// <summary>
    /// Wraps raw PCM data with a WAV header (RIFF format).
    /// Required because the Whisper pipeline expects WAV input.
    /// </summary>
    public static byte[] CreateWavData(byte[] pcmData)
    {
        var blockAlign = Channels * (BitsPerSample / 8);
        var byteRate = SampleRate * blockAlign;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // RIFF header
        writer.Write("RIFF"u8);
        writer.Write(36 + pcmData.Length);
        writer.Write("WAVE"u8);

        // fmt sub-chunk
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)Channels);
        writer.Write(SampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)BitsPerSample);

        // data sub-chunk
        writer.Write("data"u8);
        writer.Write(pcmData.Length);
        writer.Write(pcmData);

        return stream.ToArray();
    }

    /// <summary>
    /// Plays WAV audio through the default output device.
    /// Blocks until playback completes or cancellation is requested.
    /// </summary>
    public static async Task PlayAudioAsync(Stream audioStream, CancellationToken cancellationToken)
    {
        audioStream.Position = 0;
        var audioBytes = new byte[audioStream.Length];
        _ = await audioStream.ReadAsync(audioBytes, cancellationToken);

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

    /// <summary>
    /// Plays WAV audio from a byte array through the default output device.
    /// </summary>
    public static async Task PlayAudioAsync(byte[] audioData, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(audioData);
        await PlayAudioAsync(stream, cancellationToken);
    }

    /// <summary>
    /// Calculates the RMS (Root Mean Square) amplitude of 16-bit PCM audio.
    /// Used for simple voice activity detection.
    /// </summary>
    private static float CalculateRms(byte[] buffer, int bytesRecorded)
    {
        var sampleCount = bytesRecorded / 2;
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
    /// Combines multiple audio byte arrays into a single contiguous array.
    /// Used to merge streaming TTS audio chunks for playback.
    /// </summary>
    public static byte[] CombineAudioChunks(List<byte[]> chunks)
    {
        var totalLength = 0;
        foreach (var chunk in chunks)
            totalLength += chunk.Length;

        var combined = new byte[totalLength];
        var offset = 0;
        foreach (var chunk in chunks)
        {
            Buffer.BlockCopy(chunk, 0, combined, offset, chunk.Length);
            offset += chunk.Length;
        }
        return combined;
    }
}
