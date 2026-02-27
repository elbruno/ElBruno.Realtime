# Scenario 07 — Real-Time Conversation API

ASP.NET Core API with **SignalR streaming** and **REST endpoint** for real-time audio conversations using the `ElBruno.PersonaPlex.Realtime` pipeline.

## Architecture

```
Client (browser/app)
  │
  ├── POST /api/conversation/turn   →  One-shot: send WAV, get text + audio
  │
  └── WS /hubs/conversation          →  Streaming: send audio chunks, get events
          │
          ▼
   ┌──────────────────────────┐
   │ IRealtimeConversationClient │
   │                          │
   │  Silero VAD → Whisper STT │
   │       → Ollama LLM       │
   │       → QwenTTS           │
   └──────────────────────────┘
```

## Prerequisites

```bash
# Ollama with phi4-mini
ollama pull phi4-mini
ollama serve
```

## Quick Start

```bash
cd src/samples/scenario-07-realtime-api
dotnet run
```

The API starts at `http://localhost:5207`.

## REST Endpoint

### `POST /api/conversation/turn`

Send audio as multipart form data, get back transcription and AI response.

```bash
curl -X POST http://localhost:5207/api/conversation/turn \
  -F "audio=@question.wav" \
  | jq
```

**Response:**
```json
{
  "userText": "What is the capital of France?",
  "responseText": "The capital of France is Paris.",
  "processingTimeMs": 2341.5,
  "audioBase64": "UklGR...",
  "audioMediaType": "audio/wav"
}
```

## SignalR Hub

### `/hubs/conversation`

**Methods:**
- `ProcessTurn(audioBase64)` — One-shot: send base64 WAV, get text response
- `StreamConversation(audioChunks, systemPrompt?)` — Streaming: send audio chunks, receive events

**Event Types:**
| Kind | Description |
|------|-------------|
| `SpeechDetected` | VAD detected speech in audio stream |
| `TranscriptionComplete` | Whisper finished transcribing speech |
| `ResponseStarted` | LLM started generating |
| `ResponseTextChunk` | LLM produced a text token |
| `ResponseComplete` | Full turn complete |

## Code Highlights

```csharp
// Program.cs — 5 lines of setup
builder.Services.AddPersonaPlexRealtime(opts => { ... })
    .UseWhisperStt("whisper-tiny.en")
    .UseQwenTts()
    .UseSileroVad();

builder.Services.AddChatClient(new OllamaChatClient(uri, "phi4-mini"));

// ConversationHub.cs — inject and use
public class ConversationHub : Hub
{
    public ConversationHub(IRealtimeConversationClient conversation) { ... }
}
```
