# Scenario 04 — Real-Time Microphone Conversation

Console app that captures audio from your microphone and has a continuous conversation with an LLM: **Microphone → Whisper STT → Ollama LLM → Text Response → Loop**.

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
Microphone → [Whisper STT] → Text → [Ollama LLM] → Console Text Response → Loop
```

1. Lists available microphones and selects the default device
2. Listens for speech from the microphone
3. Detects silence (1.5s pause) to know when you've finished speaking
4. **Whisper** transcribes the captured audio to text (auto-downloads `whisper-tiny.en` on first run)
5. **Ollama** generates a response using `phi4-mini`
6. Prints the response to console and loops back to listening

No TTS — responses are printed to the console as text.

## Output

```
🎙️  Available microphones:
   [0] Microphone (Realtek Audio)
   Using device [0]

✅ Pipeline initialized
   STT:  Whisper tiny.en (auto-download on first use)
   LLM:  Ollama phi4-mini (localhost:11434)
   TTS:  None (text output only)

🎤 Listening... (speak, then pause for 1.5s to process)
🔄 Processing...
📝 You said: What is the capital of France?
🤖 AI: The capital of France is Paris.
⏱️  2.3s

🎤 Listening... (speak, then pause for 1.5s to process)
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
