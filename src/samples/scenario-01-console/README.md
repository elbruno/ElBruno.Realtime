# Scenario 01 — Real-Time Console Conversation

Minimal console app demonstrating the `ElBruno.PersonaPlex.Realtime` pipeline: **Audio → STT → LLM → TTS**.

## Prerequisites

1. **Ollama** running locally with `phi4-mini`:
   ```bash
   ollama pull phi4-mini
   ollama serve
   ```

2. A **16kHz mono WAV** audio file with a spoken question.

## Quick Start

```bash
# From the repository root
cd src/samples/scenario-01-console

# Run with an audio file
dotnet run -- path/to/question.wav
```

## What It Does

```
Audio File → [Whisper STT] → Text → [Ollama LLM] → Response → [TTS] → Audio Response
```

1. **Whisper** transcribes the input WAV to text (auto-downloads `whisper-tiny.en` on first run)
2. **Ollama** generates a response using `phi4-mini`
3. **TTS** synthesizes the response as speech (auto-downloads models on first run)

## Output

```
📂 Model locations:
   Whisper: ✅ Found at C:\Users\...\whisper-models\ggml-tiny.en.bin (75 MB)
   TTS:     Auto-downloaded by QwenTTS on first use

✅ Pipeline initialized
   STT:  Whisper tiny.en
   LLM:  Ollama phi4-mini (localhost:11434)
   TTS:  QwenTTS

📁 Input: question.wav
🔄 Processing...

📝 User said: What is the capital of France?
🤖 AI replied: The capital of France is Paris.
⏱️  Processing time: 2.3s
🔊 Audio response: response_question.wav
```

## Code Highlights

```csharp
// Configure the pipeline
services.AddPersonaPlexRealtime(opts => { ... })
    .UseWhisperStt("whisper-tiny.en");

services.AddQwenTts();
services.AddSingleton<ITextToSpeechClient, QwenTextToSpeechClientAdapter>();

// Process a turn
var turn = await conversation.ProcessTurnAsync(audioStream);
```

## Switching Models

```csharp
// Use a more accurate Whisper model
.UseWhisperStt("whisper-base.en")  // 142MB instead of 75MB

// Use a different LLM
services.AddChatClient(new OllamaChatClient(uri, "llama3.2"));
```
