# Models Overview — How Each Model Is Used

This document describes every AI model used in the ElBruno.PersonaPlex ecosystem, what role it plays in the pipeline, where it comes from, and how it's loaded.

## Architecture at a Glance

```
                         ┌───────────────────────────────────────────┐
                         │          User's Microphone / Audio        │
                         └──────────────────┬────────────────────────┘
                                            │ raw PCM audio
                                            ▼
                    ┌───────────────────────────────────────────────┐
                    │  🔇 Silero VAD v5 (Voice Activity Detection)  │
                    │  Detects speech vs silence in audio stream     │
                    │  Model: silero_vad.onnx (~2 MB)               │
                    └──────────────────┬────────────────────────────┘
                                       │ speech segments only
                                       ▼
                    ┌───────────────────────────────────────────────┐
                    │  🎙️ Whisper (Speech-to-Text)                  │
                    │  Transcribes speech segments to text           │
                    │  Model: ggml-tiny.en (~75 MB)                 │
                    └──────────────────┬────────────────────────────┘
                                       │ transcribed text
                                       ▼
                    ┌───────────────────────────────────────────────┐
                    │  🤖 Phi4-Mini via Ollama (LLM Chat)           │
                    │  Generates conversational response text        │
                    │  Model: phi4-mini (~2.7 GB)                   │
                    │  Interface: IChatClient (M.E.AI)              │
                    └──────────────────┬────────────────────────────┘
                                       │ response text
                                       ▼
                    ┌───────────────────────────────────────────────┐
                    │  🗣️ QwenTTS (Text-to-Speech)                  │
                    │  Synthesizes response text into audio          │
                    │  Model: Qwen3-TTS ONNX (~5.5 GB)             │
                    └──────────────────┬────────────────────────────┘
                                       │ WAV audio
                                       ▼
                         ┌───────────────────────────────────────────┐
                         │          Speaker / Audio Output            │
                         └───────────────────────────────────────────┘

── Offline / Direct Inference (Scenarios 01-03) ──────────────────────

                    ┌───────────────────────────────────────────────┐
                    │  🧠 NVIDIA PersonaPlex 7B v1 (Moshi Codec)    │
                    │  Full-duplex speech-to-speech transformer      │
                    │  mimi_encoder.onnx (178 MB) — audio → tokens  │
                    │  mimi_decoder.onnx (170 MB) — tokens → audio  │
                    │  lm_backbone.onnx (13.3 GB) — transformer LM  │
                    └───────────────────────────────────────────────┘
```

---

## Model Details

### 1. 🔇 Silero VAD v5 — Voice Activity Detection

| Property | Value |
|----------|-------|
| **Role** | Detects when the user is speaking vs. silent |
| **Why needed** | In a continuous audio stream, we only want to transcribe actual speech — not silence, background noise, or keyboard clicks |
| **Model** | `silero_vad.onnx` |
| **Size** | ~2 MB |
| **Source** | [onnx-community/silero-vad](https://huggingface.co/onnx-community/silero-vad) on HuggingFace |
| **Runtime** | ONNX Runtime (`Microsoft.ML.OnnxRuntime`) |
| **Audio format** | 16kHz, 16-bit mono PCM |
| **How it works** | Processes 512-sample windows (32ms each). Uses an RNN that maintains state across windows. Outputs a speech probability (0.0–1.0) per window. When probability exceeds threshold (default 0.5) for long enough, a speech segment is emitted. |
| **Auto-download** | Yes — `SileroModelManager.EnsureModelAsync()` downloads on first use |
| **Cache location** | `%LOCALAPPDATA%/ElBruno/PersonaPlex/silero-vad/` |
| **C# interface** | `IVoiceActivityDetector` (custom) |
| **C# class** | `SileroVadDetector` |
| **NuGet package** | `ElBruno.Realtime.SileroVad` |

**Configuration:**
```csharp
.UseSileroVad()  // defaults
// or custom thresholds:
var options = new VadOptions
{
    SpeechThreshold = 0.5f,       // probability cutoff
    MinSpeechDurationMs = 250,    // ignore speech < 250ms
    MinSilenceDurationMs = 300,   // wait 300ms silence before ending segment
    SampleRate = 16000,
};
```

---

### 2. 🎙️ Whisper — Speech-to-Text (STT)

| Property | Value |
|----------|-------|
| **Role** | Converts spoken audio into text |
| **Why needed** | The LLM works with text — we need to transcribe what the user said before sending it to the chat model |
| **Model (default)** | `ggml-tiny.en` (English-only, optimized for speed) |
| **Model (accurate)** | `ggml-base.en` (better accuracy, slightly slower) |
| **Size** | tiny.en: ~75 MB, base.en: ~142 MB |
| **Source** | Downloaded via [Whisper.net](https://github.com/sandrohanea/whisper.net) built-in downloader |
| **Runtime** | Whisper.net (GGML format, native C library) |
| **Audio format** | 16kHz, 16-bit mono PCM WAV |
| **How it works** | OpenAI's Whisper model re-packaged in GGML format. Processes audio and outputs timestamped text segments. Supports both batch (`GetTextAsync`) and streaming (`GetStreamingTextAsync`) modes. |
| **Auto-download** | Yes — `WhisperModelManager.EnsureModelAsync()` downloads on first use |
| **Cache location** | `%LOCALAPPDATA%/ElBruno/PersonaPlex/whisper-models/` |
| **C# interface** | `ISpeechToTextClient` (Microsoft.Extensions.AI, experimental) |
| **C# class** | `WhisperSpeechToTextClient` |
| **NuGet package** | `ElBruno.Realtime.Whisper` |

**Supported models:**
| Model ID | GGML Type | Size | Language | Speed |
|----------|-----------|------|----------|-------|
| `whisper-tiny.en` | TinyEn | 75 MB | English | ⚡ Fastest |
| `whisper-tiny` | Tiny | 75 MB | Multi | ⚡ Fast |
| `whisper-base.en` | BaseEn | 142 MB | English | 🔄 Balanced |
| `whisper-base` | Base | 142 MB | Multi | 🔄 Balanced |
| `whisper-small.en` | SmallEn | 466 MB | English | 🐢 Slower |
| `whisper-small` | Small | 466 MB | Multi | 🐢 Slower |
| `whisper-medium.en` | MediumEn | 1.5 GB | English | 🐌 Slow |
| `whisper-medium` | Medium | 1.5 GB | Multi | 🐌 Slow |
| `whisper-large-v3` | LargeV3 | 3.1 GB | Multi | 🐌 Slowest |

**Configuration:**
```csharp
.UseWhisperStt("whisper-tiny.en")   // fast, English-only (default)
.UseWhisperStt("whisper-base.en")   // more accurate
.UseWhisperSttFromPath("my-model.bin")  // pre-downloaded
```

---

### 3. 🤖 Phi4-Mini — Large Language Model (LLM Chat)

| Property | Value |
|----------|-------|
| **Role** | Generates conversational AI responses |
| **Why needed** | This is the "brain" — takes the transcribed user text and produces a meaningful response |
| **Model** | `phi4-mini` (Microsoft Phi-4 Mini) |
| **Size** | ~2.7 GB |
| **Source** | [Ollama](https://ollama.com) registry — `ollama pull phi4-mini` |
| **Runtime** | Ollama (local HTTP server at `http://localhost:11434`) |
| **How it works** | Small but capable language model from Microsoft. Runs as a local HTTP service via Ollama. Supports streaming responses. We send the transcribed text + system prompt + conversation history, and stream back the response token by token. |
| **Auto-download** | No — requires manual `ollama pull phi4-mini` |
| **C# interface** | `IChatClient` (Microsoft.Extensions.AI, stable) |
| **C# class** | `OllamaChatClient` (from `Microsoft.Extensions.AI.Ollama`) |
| **NuGet package** | `Microsoft.Extensions.AI.Ollama` |

> **Note:** The LLM is pluggable via `IChatClient`. You can swap phi4-mini for any model Ollama supports, or use OpenAI, Azure OpenAI, or any other `IChatClient` provider.

**Configuration:**
```csharp
// Ollama (local)
builder.Services.AddChatClient(new OllamaChatClient(
    new Uri("http://localhost:11434"), "phi4-mini"));

// Or OpenAI (cloud)
builder.Services.AddChatClient(new OpenAIChatClient(
    new OpenAIClient("sk-..."), "gpt-4o-mini"));

// Or Azure OpenAI
builder.Services.AddChatClient(new AzureOpenAIChatClient(...));
```

**Conversation history:** The pipeline maintains a rolling conversation history (default 20 messages) so the LLM has context across turns.

---

### 4. 🗣️ QwenTTS (Qwen3-TTS) — Text-to-Speech (TTS)

| Property | Value |
|----------|-------|
| **Role** | Converts the LLM's text response into spoken audio |
| **Why needed** | For a voice conversation, the AI's text response needs to be spoken aloud |
| **Model** | Qwen3-TTS (multiple ONNX files) |
| **Size** | ~5.5 GB total |
| **Source** | Downloaded via [ElBruno.QwenTTS](https://github.com/elbruno/ElBruno.QwenTTS) |
| **Runtime** | ONNX Runtime via ElBruno.QwenTTS pipeline |
| **Output format** | 24kHz WAV audio |
| **How it works** | Neural TTS model that converts text to natural-sounding speech. Supports multiple voices (e.g., "ryan", "serena") and languages. The pipeline writes to a temp WAV file, reads it into memory, and returns the audio bytes. |
| **Auto-download** | Yes — `TtsPipeline.CreateAsync()` auto-downloads on first use |
| **C# interface** | `ITextToSpeechClient` (custom, following M.E.AI patterns) |
| **C# class** | `QwenTextToSpeechClient` |
| **NuGet package** | `ElBruno.Realtime.QwenTTS` |

**Configuration:**
```csharp
.UseQwenTts()                          // default voice: "ryan"
.UseQwenTts(defaultVoice: "serena")    // different voice
.UseQwenTts(defaultLanguage: "english")
```

---

### 5. 🧠 NVIDIA PersonaPlex 7B v1 — Full-Duplex Speech-to-Speech

| Property | Value |
|----------|-------|
| **Role** | Direct audio-in → audio-out inference (no text intermediate step) |
| **Why needed** | This is the original NVIDIA model — a single model that handles the entire speech conversation loop. Used in offline/direct scenarios (01-03) |
| **Architecture** | Based on [Moshi](https://github.com/kyutai-labs/moshi) full-duplex codec-language model |
| **Components** | 3 ONNX models working together |
| **Source** | [elbruno/personaplex-7b-v1-onnx](https://huggingface.co/elbruno/personaplex-7b-v1-onnx) on HuggingFace |
| **Runtime** | ONNX Runtime (`Microsoft.ML.OnnxRuntime`) |
| **C# class** | `PersonaPlexPipeline` |
| **NuGet package** | `ElBruno.PersonaPlex` |

**Sub-models:**

| Model | File | Size | Role |
|-------|------|------|------|
| **Mimi Encoder** | `mimi_encoder.onnx` | 178 MB | Encodes raw audio waveform into discrete tokens (audio codec) |
| **Mimi Decoder** | `mimi_decoder.onnx` | 170 MB | Decodes discrete tokens back into audio waveform |
| **LM Backbone** | `lm_backbone.onnx` + `.data` | 13.3 GB (FP16) | 7B-parameter Transformer that processes encoded audio tokens, applies persona prompts, and generates response tokens |

**How it works:**
1. **Encode**: Raw audio → Mimi Encoder → discrete tokens
2. **Process**: Tokens + persona prompt → LM Backbone → response tokens
3. **Decode**: Response tokens → Mimi Decoder → spoken audio output

> **Note:** The LM backbone is 13.3 GB — too large for quick inference on most machines. This is why the Realtime pipeline uses the component approach (Whisper + LLM + QwenTTS) instead for interactive scenarios.

---

## Two Pipelines Compared

| Aspect | Realtime Pipeline (Scenarios 04-07) | Direct PersonaPlex (Scenarios 01-03) |
|--------|-------------------------------------|--------------------------------------|
| **Approach** | Component-based: separate STT → LLM → TTS | Monolithic: single model does everything |
| **Text intermediate** | ✅ Yes — text is available at every step | ❌ No — audio in, audio out (no text) |
| **Model size** | ~8.3 GB total (Whisper 75MB + Phi4 2.7GB + QwenTTS 5.5GB) | ~13.6 GB (encoder + decoder + backbone) |
| **Inference speed** | ⚡ Fast (each component is optimized) | 🐌 Slower (~0.4s/step + 25s load) |
| **Persona control** | Via LLM system prompt | Via text prompt to backbone |
| **Voice selection** | QwenTTS voices (ryan, serena, etc.) | PersonaPlex voices (NATF0-3, VARM0-4, etc.) |
| **Pluggable** | ✅ Swap any component (STT, LLM, TTS) | ❌ Monolithic model |
| **Full-duplex** | 🔮 Future (rt4-full-duplex) | ✅ Native (Moshi architecture) |
| **Best for** | Interactive conversations, web apps | Research, offline processing |

---

## Model Download Summary

| Model | Auto-Download | Manual Setup | First-Use Time |
|-------|--------------|--------------|----------------|
| Silero VAD | ✅ Automatic | — | ~2 seconds |
| Whisper tiny.en | ✅ Automatic | — | ~30 seconds |
| QwenTTS | ✅ Automatic | — | ~5-10 minutes |
| Mimi encoder/decoder | ✅ Automatic | — | ~2 minutes |
| Phi4-Mini (Ollama) | ❌ Manual | `ollama pull phi4-mini` | ~5 minutes |
| LM Backbone | ❌ Manual export | `python export_onnx.py` | ~30 minutes |

All auto-downloaded models are cached in `%LOCALAPPDATA%/ElBruno/PersonaPlex/` and shared across all apps using the library.
