# Routing Rules

## Domain Routing

| Domain / Signal | Primary Agent | Secondary |
|----------------|---------------|-----------|
| Pipeline architecture, M.E.AI interfaces, abstractions | Ripley | Dallas |
| C# implementation, providers, DI, streaming | Dallas | Ripley |
| ONNX models, Whisper, QwenTTS, Silero VAD | Dallas | Ripley |
| Blazor UI, browser audio, WebRTC, SignalR | Lambert | Dallas |
| Samples, demos, scenario apps | Lambert | Dallas |
| Tests, xUnit, quality, edge cases | Kane | Dallas |
| Security review, input validation, path traversal | Kane | Ripley |
| CI/CD, GitHub Actions, NuGet publishing | Parker | Ripley |
| Documentation, README, API docs | Parker | Lambert |
| Version management, .props, packaging | Parker | Dallas |

## Name Routing

| If user says... | Route to |
|-----------------|----------|
| "Ripley" | Ripley (Lead / Architect) |
| "Dallas" | Dallas (C# Developer) |
| "Lambert" | Lambert (Frontend Developer) |
| "Kane" | Kane (Tester) |
| "Parker" | Parker (DevOps) |
| "team" | Fan-out to relevant agents |
