# Decision: Scenario 04 TTS Implementation Pattern

**Date:** 2026-02-28  
**By:** Lambert (Frontend Developer)  
**Status:** ✅ Implemented

## Context

Scenario-04-realtime-console was text-only output (no TTS). User requested adding voice responses to complete the full voice conversation loop: microphone → STT → LLM → TTS → speakers.

## Decision

Adopted the same TTS integration pattern from scenario-01-console:

1. **Package:** `ElBruno.QwenTTS` version `0.1.8-preview`
2. **Adapter Pattern:** Copy `QwenTextToSpeechClientAdapter.cs` (adapts `ITtsPipeline` to `ITextToSpeechClient`)
3. **DI Registration:** `services.AddQwenTts()` + `services.AddSingleton<ITextToSpeechClient, QwenTextToSpeechClientAdapter>()`
4. **Audio Playback:** NAudio's `WaveOutEvent` + `WaveFileReader` with blocking playback loop

## Rationale

- **Consistency:** Matches scenario-01 implementation — no need to invent new patterns
- **Simplicity:** QwenTTS handles model download, voice selection, WAV generation automatically
- **NAudio Integration:** Already a dependency for microphone capture, reuse for playback
- **Blocking Playback:** Must wait for audio to finish before listening again (prevents feedback loop)

## Implementation Details

### Audio Playback Function
```csharp
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
```

### Pipeline Flow
1. Capture microphone audio until silence (1.5s pause)
2. Transcribe with Whisper → show "You said:" immediately
3. Send to Ollama LLM → show "AI replied:" after response
4. Synthesize with QwenTTS → show "🔊 Playing audio response..."
5. Play audio through speakers (blocking)
6. Loop back to listening

## User Experience Enhancements

### Timestamps
Added `Log(string message)` helper that prefixes all console output with `[HH:mm:ss]` timestamps. This helps:
- Track conversation timing
- Debug pipeline performance issues
- Understand where delays occur (STT vs LLM vs TTS)

### Progress Indicators
Changed from single "🔄 Processing..." to staged output:
- `🔄 Transcribing...` — after silence detected, before STT completes
- `📝 You said: <text>` — immediately after STT, before LLM call
- `🤖 AI replied: <text>` — after LLM response
- `🔊 Playing audio response...` — before TTS playback
- `⏱️ Total: 6.2s` — after everything completes

This creates a more responsive feel — user sees their transcription ASAP, not waiting for full pipeline.

## Alternatives Considered

### Why Not Streaming TTS?
QwenTTS library doesn't support true streaming — it synthesizes the entire text to a temp file, then returns. The `GetStreamingSpeechAsync` implementation is a wrapper that yields the complete audio as a single chunk. For this console app, blocking playback is simpler and acceptable.

### Why Not Async Playback?
Tried async (fire-and-forget) playback initially, but this causes:
- Feedback loop: microphone picks up speaker output while listening
- Overlapping audio if user speaks during TTS playback
Blocking playback ensures clean turn-taking: listen → process → speak → listen.

## Impact

- **Positive:** Full voice conversation loop — hands-free operation possible
- **Positive:** Better UX with timestamps and staged progress indicators
- **Neutral:** One CA2022 warning about `Stream.ReadAsync` (inexact read) — acceptable
- **Consideration:** QwenTTS auto-downloads models on first run (~100-200MB) — documented in README

## Files Modified

- `src/samples/scenario-04-realtime-console/QwenTextToSpeechClientAdapter.cs` (new)
- `src/samples/scenario-04-realtime-console/Program.cs` (modified)
- `src/samples/scenario-04-realtime-console/scenario-04-realtime-console.csproj` (modified)
- `src/samples/scenario-04-realtime-console/README.md` (modified)

## Build Status

✅ Build succeeded with 1 warning (CA2022)
