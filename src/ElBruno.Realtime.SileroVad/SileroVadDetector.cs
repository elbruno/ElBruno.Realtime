using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Runtime.CompilerServices;

namespace ElBruno.Realtime.SileroVad;

/// <summary>
/// An <see cref="IVoiceActivityDetector"/> implementation using Silero VAD v5 via ONNX Runtime.
/// Automatically downloads the model on first use.
/// </summary>
/// <remarks>
/// Silero VAD operates on 16kHz, 16-bit mono PCM audio with a 512-sample window (32ms).
/// It maintains internal RNN state across calls for context-aware speech detection.
/// </remarks>
public class SileroVadDetector : IVoiceActivityDetector
{
    private readonly string? _cacheDir;
    private InferenceSession? _session;
    private bool _disposed;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _inferenceLock = new(1, 1);

    // Silero VAD constants
    private const int DefaultSampleRate = 16000;
    private const int WindowSizeSamples = 512; // 32ms at 16kHz

    /// <summary>
    /// Creates a new <see cref="SileroVadDetector"/>.
    /// </summary>
    /// <param name="cacheDir">Optional directory for caching the downloaded model.</param>
    public SileroVadDetector(string? cacheDir = null)
    {
        _cacheDir = cacheDir;
    }

    /// <summary>
    /// Creates a new <see cref="SileroVadDetector"/> from a pre-downloaded model file.
    /// </summary>
    public static SileroVadDetector FromModelPath(string modelPath)
    {
        var detector = new SileroVadDetector();
        detector._session = new InferenceSession(modelPath);
        return detector;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SpeechSegment> DetectSpeechAsync(
        IAsyncEnumerable<byte[]> audioChunks,
        VadOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await EnsureInitializedAsync(cancellationToken);

        var sampleRate = options?.SampleRate ?? DefaultSampleRate;
        var speechThreshold = options?.SpeechThreshold ?? 0.5f;
        var minSpeechMs = options?.MinSpeechDurationMs ?? 250;
        var minSilenceMs = options?.MinSilenceDurationMs ?? 300;

        // Internal state for Silero VAD
        var state = new float[2 * 1 * 128]; // 2 layers, 1 batch, 128 hidden
        var sr = new long[] { sampleRate };

        // Speech segment accumulation
        var audioBuffer = new List<byte>();
        var speechBuffer = new List<float>(); // raw samples of speech segment
        bool inSpeech = false;
        int silenceSamples = 0;
        int speechStartSample = 0;
        int totalSamplesProcessed = 0;
        int minSilenceSamples = (int)(minSilenceMs / 1000.0 * sampleRate);
        int minSpeechSamples = (int)(minSpeechMs / 1000.0 * sampleRate);

        // Buffer for accumulating PCM data until we have enough for a window
        var pcmAccumulator = new List<float>();

        await foreach (var chunk in audioChunks.WithCancellation(cancellationToken))
        {
            // Convert byte[] (16-bit PCM) to float[]
            var floatSamples = ConvertBytesToFloat(chunk);
            pcmAccumulator.AddRange(floatSamples);

            // Process in windows of 512 samples
            while (pcmAccumulator.Count >= WindowSizeSamples)
            {
                var window = pcmAccumulator.GetRange(0, WindowSizeSamples).ToArray();
                pcmAccumulator.RemoveRange(0, WindowSizeSamples);

                await _inferenceLock.WaitAsync(cancellationToken);
                float probability;
                try
                {
                    probability = RunInference(window, state, sr);
                }
                finally
                {
                    _inferenceLock.Release();
                }

                if (probability >= speechThreshold)
                {
                    if (!inSpeech)
                    {
                        inSpeech = true;
                        speechStartSample = totalSamplesProcessed;
                        speechBuffer.Clear();
                    }
                    speechBuffer.AddRange(window);
                    silenceSamples = 0;
                }
                else
                {
                    if (inSpeech)
                    {
                        silenceSamples += WindowSizeSamples;
                        speechBuffer.AddRange(window); // Include trailing silence

                        if (silenceSamples >= minSilenceSamples)
                        {
                            // End of speech segment
                            if (speechBuffer.Count >= minSpeechSamples)
                            {
                                yield return CreateSegment(speechBuffer, speechStartSample, sampleRate);
                            }
                            inSpeech = false;
                            speechBuffer.Clear();
                        }
                    }
                }

                totalSamplesProcessed += WindowSizeSamples;
            }
        }

        // Emit any remaining speech
        if (inSpeech && speechBuffer.Count >= minSpeechSamples)
        {
            yield return CreateSegment(speechBuffer, speechStartSample, sampleRate);
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(SileroVadDetector) || serviceType == typeof(IVoiceActivityDetector))
            return this;

        return null;
    }

    private float RunInference(float[] audioWindow, float[] state, long[] sr)
    {
        var inputTensor = new DenseTensor<float>(audioWindow, [1, WindowSizeSamples]);
        var stateTensor = new DenseTensor<float>(state, [2, 1, 128]);
        var srTensor = new DenseTensor<long>(sr, [1]);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", inputTensor),
            NamedOnnxValue.CreateFromTensor("state", stateTensor),
            NamedOnnxValue.CreateFromTensor("sr", srTensor),
        };

        using var results = _session!.Run(inputs);
        var resultList = results.ToList();

        // Output: "output" (speech probability), "stateN" (updated state)
        var output = resultList[0].AsTensor<float>();
        var newState = resultList[1].AsTensor<float>();

        // Update state in-place
        int idx = 0;
        foreach (var v in newState)
            state[idx++] = v;

        return output[0];
    }

    private static SpeechSegment CreateSegment(List<float> samples, int startSample, int sampleRate)
    {
        // Convert float samples back to 16-bit PCM bytes
        var audioData = new byte[samples.Count * 2];
        for (int i = 0; i < samples.Count; i++)
        {
            var s = (short)(Math.Clamp(samples[i], -1f, 1f) * 32767);
            audioData[i * 2] = (byte)(s & 0xFF);
            audioData[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }

        var startTime = TimeSpan.FromSeconds((double)startSample / sampleRate);
        var endTime = TimeSpan.FromSeconds((double)(startSample + samples.Count) / sampleRate);

        return new SpeechSegment
        {
            AudioData = audioData,
            StartTime = startTime,
            EndTime = endTime,
            Confidence = 1.0f, // Silero gives per-window, we approximate
        };
    }

    private static float[] ConvertBytesToFloat(byte[] pcm16)
    {
        var floats = new float[pcm16.Length / 2];
        for (int i = 0; i < floats.Length; i++)
        {
            short sample = (short)(pcm16[i * 2] | (pcm16[i * 2 + 1] << 8));
            floats[i] = sample / 32768f;
        }
        return floats;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_session is not null) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_session is not null) return;

            var modelPath = await SileroModelManager.EnsureModelAsync(
                _cacheDir, cancellationToken);
            _session = new InferenceSession(modelPath);
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _session?.Dispose();
        _session = null;
        _initLock.Dispose();
        _inferenceLock.Dispose();

        GC.SuppressFinalize(this);
    }
}
