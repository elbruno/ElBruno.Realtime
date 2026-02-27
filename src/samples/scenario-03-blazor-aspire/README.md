# Scenario 04 — Blazor + Aspire + Ollama Multi-Service Conversation

A real-time conversation app featuring **dual Blazor frontends** (voice chat + game) sharing a **single Aspire-managed API backend** with **Ollama-powered** AI, demonstrating microservice architecture with shared infrastructure.

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                  .NET Aspire AppHost (scenario-04.AppHost)        │
│         (Orchestration · Discovery · Dashboard · Telemetry)       │
└──┬────────────────────┬──────────────────┬──────────────────┬────┘
   │                    │                  │                  │
   ▼                    ▼                  ▼                  ▼
┌─────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────┐
│  Web Svc    │  │  Game Svc    │  │  API Backend │  │  Ollama  │
│  Blazor     │  │  Blazor      │  │ (ASP.NET     │  │Container │
│  Server     │  │  Server      │  │  Core)       │  │phi4-mini │
│             │  │              │  │              │  │          │
│ Convers.    │  │ Game.razor + │  │ ConversHub   │  │REST API  │
│ razor       │  │ game-engine  │  │ GameHub      │  │:11434    │
│ (voice)     │  │ .js          │  │ M.E.AI       │  │          │
└─────┬───────┘  └──────┬───────┘  └──────┬───────┘  └────┬─────┘
      │                 │                 │              │
      │ SignalR         │ SignalR         │ Ollama API   │
      │ (discovery)     │ (discovery)     │ (OpenAI-compat)
      └─────────────────┼─────────────────┤              │
                        │                 │              │
                        └────────────────►┴─────────────►┘
```

### Data Flow

```
Voice Chat (Web):              Game Commands (Game):
User speaks via mic            Player input (keyboard/mouse)
    │                                │
    ▼                                ▼
Blazor Web (SignalR)  ┐      Blazor Game (SignalR)
    └────────┬────────┴──────────────┬────────┘
             ▼                       ▼
        API Backend (scenario-04.Api)
        - ConversationHub (voice)
        - GameHub (game state)
             │
             ▼
        Ollama (phi4-mini)
             │
    ┌────────┴────────┐
    ▼                 ▼
Response text      Game logic feedback
    │                 │
Blazor Web         Blazor Game
streams audio      updates canvas
```

## Prerequisites

1. **.NET 10 SDK** (or .NET 9 SDK)
2. **Ollama** installed and running locally — [ollama.com](https://ollama.com)
3. **phi4-mini model** pulled: `ollama pull phi4-mini`

## How to Run

```bash
# 1. Start Ollama (if not already running):
ollama serve

# 2. Pull the model (first time only):
ollama pull phi4-mini

# 3. From the repo root:
cd src/samples/scenario-04-blazor-aspire

# 4. Run the Aspire AppHost (starts API + Web + Game):
dotnet run --project scenario-04.AppHost
```

### What happens when you run it:

1. **Aspire starts the API backend** — connects to Ollama at `http://localhost:11434`, exposes ConversationHub + GameHub
2. **Aspire starts the Web frontend** — voice chat UI, connects to API via SignalR
3. **Aspire starts the Game frontend** — side-scroller game, connects to API via SignalR  
4. **Aspire Dashboard opens** — shows all three services, logs, traces

### Using Docker-managed Ollama (optional)

If you prefer Aspire to manage Ollama via Docker instead of running it locally, edit `scenario-04.AppHost/Program.cs` and uncomment the Docker-based Ollama section. This requires Docker Desktop to be running.

## Using the App

### Web Frontend (Voice Chat)

1. Open the **Web** service endpoint from the Aspire dashboard (or the URL printed in console)
2. Navigate to `/conversation`
3. Type a message or use voice modes (see below)
4. Watch the AI response stream in real-time, token by token

### Game Frontend (Side-Scroller)

1. Open the **Game** service endpoint from the Aspire dashboard
2. Navigate to `/game`
3. Use keyboard/mouse controls to play
4. Game logic is powered by the shared API backend

### Features (Voice Chat)

- **Streaming responses** — tokens appear as Ollama generates them
- **Multi-turn conversation** — context is maintained across messages
- **Custom persona** — set a system prompt (e.g., "You are a pirate captain")
- **Session management** — clear history and start fresh
- **Connection status** — visual indicator for SignalR connection health
- **🗣️ Speak Mode** — always-on microphone with automatic turn detection (GPT-Realtime-like hands-free conversation)
- **🎤 Push-to-talk** — single utterance voice input
- **🔊 Auto-speak** — AI responses spoken aloud via browser TTS

### Voice Modes

| Mode | How it works | Best for |
|------|-------------|----------|
| **Text** | Type and press Enter/Send | Normal chat |
| **Push-to-talk (🎤)** | Click mic → speak → auto-sends on pause | Quick voice input |
| **Speak Mode (🗣️)** | Click to enter always-on mode. Mic stays open, auto-sends on each pause, AI speaks response, mic resumes listening. Click 🔴 or ⏹️ Stop to exit. | Hands-free conversation |

In Speak Mode, the state indicator shows:
- 🟢 **Listening** — mic is open, waiting for you to speak
- 🎤 **Hearing you...** — speech detected, transcribing
- ⏳ **Processing** — sending to Ollama
- 🔊 **Speaking** — AI is responding (interrupt by speaking again)

## Key Technology Choices

| Component | Technology | Version | Why |
|-----------|-----------|---------|-----|
| Frontend | **Blazor Server** | .NET 10 | SignalR built-in, server-side rendering |
| Communication | **SignalR + MessagePack** | 10.0.3 | Binary streaming, auto-reconnect |
| AI Framework | **Microsoft Agent Framework** | 1.0.0-rc2 | `AIAgent` + `OllamaChatClient` ([docs](https://learn.microsoft.com/agent-framework/agents/providers/ollama)) |
| AI Abstractions | **Microsoft.Extensions.AI.Ollama** | 9.7.0-preview | `OllamaChatClient` as `IChatClient` |
| LLM | **Ollama (phi4-mini)** | latest | 3.8B params, fast, runs locally |
| Orchestration | **.NET Aspire** | 13.1.2 | Service discovery, telemetry, container management |

## Microsoft Agent Framework Integration

This scenario follows the [official Microsoft Agent Framework + Ollama pattern](https://learn.microsoft.com/agent-framework/agents/providers/ollama).

### How it works

**1. Register OllamaChatClient as IChatClient (Program.cs):**

```csharp
// Microsoft.Extensions.AI.Ollama provides OllamaChatClient
builder.Services.AddChatClient(new OllamaChatClient(
        new Uri(ollamaEndpoint), ollamaModel))
    .UseFunctionInvocation()    // Enable function/tool calling
    .UseOpenTelemetry()         // Traces visible in Aspire dashboard
    .UseLogging();              // Log all AI interactions
```

**2. One-shot agent query (Agent Framework pattern):**

```csharp
using Microsoft.Agents.AI;

// Create an AIAgent from the IChatClient — this is the Agent Framework pattern
var agent = chatClient.AsAIAgent(
    instructions: "You are a helpful assistant running locally via Ollama.");

var result = await agent.RunAsync("What is the largest city in France?");
Console.WriteLine(result.Text);
```

**3. Multi-turn streaming conversation (ConversationService):**

```csharp
// For multi-turn chat, we manage history per session and stream tokens
await foreach (var token in chatClient.GetStreamingResponseAsync(chatHistory))
{
    yield return token.Text;  // Stream each token to the Blazor UI via SignalR
}
```

### Packages used

```xml
<PackageReference Include="Microsoft.Extensions.AI" Version="10.3.0" />
<PackageReference Include="Microsoft.Extensions.AI.Ollama" Version="9.7.0-preview.1.25356.2" />
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-rc2" />
```

## Changing the Ollama Model

Pull a different model and update `scenario-04.Api/appsettings.json` (or set the `Ollama:Model` config):

```bash
ollama pull llama3.2
```

Then set the model name in the API config or environment variable:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "llama3.2"
  }
}
```

Popular options:
| Model | Size | Speed | Quality |
|-------|------|-------|---------|
| `phi4-mini` | ~2.5 GB | ⚡ Fast | Good |
| `llama3.2` | ~2 GB | ⚡ Fast | Good |
| `llama3.1:8b` | ~4.7 GB | Medium | Better |
| `phi4` | ~9 GB | Slower | Best |

## Project Structure

```
scenario-03-blazor-aspire/
├── scenario-04.AppHost/           # Aspire orchestrator
│   └── Program.cs                 # Ollama + API + Web + Game wiring
├── scenario-04.ServiceDefaults/   # Shared telemetry/health
│   └── Extensions.cs
├── scenario-04.Api/               # ASP.NET Core shared backend
│   ├── Program.cs                 # DI, SignalR, M.E.AI setup
│   ├── Hubs/
│   │   ├── ConversationHub.cs     # SignalR hub (voice chat)
│   │   └── GameHub.cs             # SignalR hub (game state)
│   └── Services/
│       ├── ConversationService.cs # Multi-turn chat with Ollama
│       └── GameService.cs         # Game logic with Ollama reasoning
├── scenario-04.Web/               # Blazor Server voice chat frontend
│   ├── Program.cs
│   ├── Components/
│   │   ├── App.razor
│   │   ├── Routes.razor
│   │   ├── Layout/MainLayout.razor
│   │   └── Pages/
│   │       ├── Index.razor        # Home page
│   │       └── Conversation.razor # Voice chat UI
│   └── wwwroot/css/app.css
├── scenario-04.Game/              # Blazor Server game frontend (NEW)
│   ├── Program.cs
│   ├── Components/
│   │   ├── App.razor
│   │   ├── Routes.razor
│   │   └── Pages/
│   │       ├── Index.razor        # Home page
│   │       └── Game.razor         # Game UI
│   └── wwwroot/
│       ├── css/app.css
│       └── js/game-engine.js      # Game canvas & input handling
└── scenario-04.Shared/            # Shared DTOs across all services
    └── Models/
        ├── AudioChunkDto.cs
        ├── ChatMessageDto.cs
        ├── ConversationStateDto.cs
        ├── GameStateDto.cs        # Game state (player position, enemies, etc.)
        └── GameCommandDto.cs      # Game input (move, attack, etc.)
```

## Future: PersonaPlex Audio Integration

When the PersonaPlex ONNX models are fully exported, this scenario will be extended to support:

```
User speaks → Mimi Encoder → Ollama reasoning → Mimi Decoder → AI speaks back
```

The `ConversationHub.ProcessAudio()` method has a placeholder ready for this integration. See the [evaluation document](../../../docs/scenario-04-blazor-aspire-evaluation.md) for the full roadmap.

