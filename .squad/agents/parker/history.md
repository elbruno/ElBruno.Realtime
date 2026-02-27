# Parker — History

## Project Context
- **Project:** ElBruno.Realtime — Pluggable real-time audio conversation framework for .NET
- **Stack:** C#, .NET 8/10, GitHub Actions, NuGet
- **User:** Bruno Capuano
- **Description:** Local voice conversations — VAD → STT → LLM → TTS pipeline. No cloud dependencies.

## Learnings

<!-- Append learnings below. Format: ### YYYY-MM-DD: Topic\nWhat was learned. -->

### 2025-01-17: README.md Update - MEAI & Agent Framework
Added prominent "Powered By" section to README.md highlighting Microsoft.Extensions.AI and Microsoft Agent Framework as core components. MEAI provides unified chat/STT abstractions (ISpeechToTextClient, IChatClient), while Agent Framework manages conversation sessions and per-user state. These are essential to ElBruno.Realtime's pluggable architecture and real-time conversation capabilities. Minimal edit approach preserved existing content while naturally integrating new framework mentions.

### 2025-01-XX: Phase 6 - Aspire Restructure Documentation
Updated scenario-03-blazor-aspire documentation to reflect 3-service microservice architecture:
- **Title & description**: Now emphasizes "dual Blazor frontends" (voice chat + game) on shared API backend
- **Architecture diagram**: Expanded from 2 boxes (Web + API + Ollama) to 4 boxes showing Web service, Game service (NEW), API backend, and Ollama  
- **Data flow**: Dual parallel flows showing both voice chat and game commands routing through shared API + ConversationHub/GameHub
- **How to Run**: Updated to mention AppHost starts 3 services (API + Web + Game) with proper service descriptions
- **Project Structure**: Added scenario-04.Game folder with Game.razor, game-engine.js, and new Game-related DTOs (GameStateDto, GameCommandDto)
- **Using the App**: Split into Web Frontend and Game Frontend sections with clear instructions for accessing each via Aspire dashboard
- **Root README.md**: Updated sample table to describe scenario-03 as "dual frontends (voice chat + game) on shared API backend"
