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
