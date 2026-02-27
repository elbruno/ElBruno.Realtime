# Scenario 04 — Real-Time Microphone Conversation

Console app that captures audio from your microphone and has a continuous conversation with an LLM: **Microphone → Whisper STT → Ollama LLM → QwenTTS → Speakers**.

## Prerequisites

1. **Ollama** running locally with `phi4-mini`:
   ```bash
   ollama pull phi4-mini
   ollama serve
   ```

2. A working **microphone** connected to your system.

## Quick Start

```bash
# From the repository root
cd src/samples/scenario-04-realtime-console

dotnet run
```

## What It Does

```
Microphone → [Whisper STT] → Text → [Ollama LLM] → [QwenTTS] → Speakers → Loop
```

1. Lists available microphones and selects the default device
2. Listens for speech from the microphone
3. Detects silence (1.5s pause) to know when you've finished speaking
4. **Whisper** transcribes the captured audio to text (auto-downloads `whisper-tiny.en` on first run)
5. Shows the transcribed text immediately ("You said: ...")
6. **Ollama** generates a response using `phi4-mini`
7. **QwenTTS** converts the response to speech (auto-downloads on first use)
8. Plays the audio response through your speakers
9. Loops back to listening

All console output includes timestamps in `[HH:mm:ss]` format.

## Output

```
🎙️  Available microphones:
   [0] Microphone (Realtek Audio)
   Using device [0]

[14:32:14] 📂 Model locations:
   Whisper: ✅ Found at C:\Users\you\AppData\Local\ElBruno\PersonaPlex\whisper-models\ggml-tiny.en.bin (75 MB)
   LLM:     Ollama phi4-mini (ensure 'ollama serve' is running)
   TTS:     Auto-downloaded by QwenTTS on first use

[14:32:15] ✅ Pipeline initialized
   STT:  Whisper tiny.en (auto-download on first use)
   LLM:  Ollama phi4-mini (localhost:11434)
   TTS:  QwenTTS

[14:32:15] Press Ctrl+C to exit.

[14:32:15] 🎤 Listening... (speak, then pause for 1.5s to process)
[14:32:18] 🔄 Transcribing...
[14:32:19] 📝 You said: What is the capital of France?
[14:32:21] 🤖 AI replied: The capital of France is Paris.
[14:32:21] 🔊 Playing audio response...
[14:32:24] ⏱️  Total: 6.2s

[14:32:24] 🎤 Listening... (speak, then pause for 1.5s to process)
```

Press **Ctrl+C** to exit the conversation loop.

## Audio Format

The microphone captures audio at **16kHz, 16-bit mono PCM** — this is the format the Whisper pipeline expects. The raw PCM data is wrapped with a WAV header before being sent to the pipeline.

## Switching Models

```csharp
// Use a more accurate Whisper model
.UseWhisperStt("whisper-base.en")  // 142MB instead of 75MB

// Use a different LLM
services.AddChatClient(new OllamaChatClient(uri, "llama3.2"));
```
