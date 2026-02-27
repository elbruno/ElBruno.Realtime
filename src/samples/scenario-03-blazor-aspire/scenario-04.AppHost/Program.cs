// ──────────────────────────────────────────────────────────────────────────
// PersonaPlex Scenario 04 — Aspire AppHost
// ──────────────────────────────────────────────────────────────────────────
// Orchestrates:
//   1. API Backend         – ASP.NET Core + SignalR + M.E.AI → Ollama
//   2. Blazor Web Frontend – conversation UI
//
// PREREQUISITES:
//   - Ollama must be installed and running locally (http://localhost:11434)
//   - The phi4-mini model must be pulled: ollama pull phi4-mini
//
// HOW TO RUN:
//   1. Start Ollama locally:  ollama serve
//   2. Pull the model:        ollama pull phi4-mini
//   3. dotnet run --project scenario-04.AppHost
//   4. Open the Aspire dashboard (URL printed in console)
//   5. Click the Blazor Web endpoint to open the conversation UI
//
// The Aspire dashboard gives you:
//   • Service discovery (API endpoint auto-injected into Web)
//   • OpenTelemetry traces (see latency for each Ollama call)
//   • Health checks for all services
//   • Logs from all services in one place
// ──────────────────────────────────────────────────────────────────────────

var builder = DistributedApplication.CreateBuilder(args);

// ──────────────────────────────────────────────────────────────
// Ollama — runs locally (not managed by Aspire)
// ──────────────────────────────────────────────────────────────
// By default, Ollama runs locally at http://localhost:11434.
// Start it with: ollama serve
// Pull a model:  ollama pull phi4-mini
//
// OPTIONAL: If you prefer Aspire to manage Ollama via Docker,
// uncomment the lines below and comment out the api definition
// that follows. This requires Docker Desktop to be running.
//
// var ollama = builder.AddOllama("ollama")
//     .WithDataVolume("personaplex-ollama-data")
//     .AddModel("phi4-mini");
//
// var api = builder.AddProject<Projects.scenario_04_Api>("api")
//     .WithReference(ollama)
//     .WaitFor(ollama)
//     .WithExternalHttpEndpoints();
// ──────────────────────────────────────────────────────────────

// ──────────────────────────────────────────────────────────────
// API Backend — PersonaPlex + SignalR + Ollama integration
// ──────────────────────────────────────────────────────────────
// Connects to Ollama at http://localhost:11434 (default).
// Override with Ollama:Endpoint in appsettings.json if needed.
// ──────────────────────────────────────────────────────────────
var api = builder.AddProject<Projects.scenario_04_Api>("api")
    .WithExternalHttpEndpoints();

// ──────────────────────────────────────────────────────────────
// 3. Blazor Web Frontend — conversation UI
// ──────────────────────────────────────────────────────────────
// References the API backend for SignalR connection.
// Aspire injects the API endpoint as a connection string.
// ──────────────────────────────────────────────────────────────
builder.AddProject<Projects.scenario_04_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
