# Decision: Fix Aspire Frontend-to-API Connection Issues

**Author:** Dallas (C# Developer)  
**Date:** 2025-07-16  
**Status:** Implemented  

## Context

The scenario-03 Aspire sample had two connection problems:

1. **HTTPS redirection warnings** in Web and Game frontends — `app.UseHttpsRedirection()` ran unconditionally, but Aspire serves these on HTTP only, producing `Failed to determine the https port for redirect` warnings and potentially interfering with Blazor Server's internal SignalR circuit.

2. **CORS incompatible with SignalR credentials** — The API used `AllowAnyOrigin()` which cannot be combined with `AllowCredentials()`. SignalR WebSocket connections require credentials support for proper handshake.

## Changes

### Fix 1: Web & Game frontends (`scenario-04.Web/Program.cs`, `scenario-04.Game/Program.cs`)
Moved `app.UseHttpsRedirection()` inside the `if (!app.Environment.IsDevelopment())` block so it only runs in production, not under Aspire's HTTP-only development orchestration.

### Fix 2: API backend (`scenario-04.Api/Program.cs`)
Replaced `AllowAnyOrigin()` with `SetIsOriginAllowed(_ => true)` + `AllowCredentials()` so SignalR WebSocket connections can properly authenticate and maintain persistent connections.

## Rationale

- These are the minimal changes needed to fix the connection churn.
- The HTTPS redirect fix follows ASP.NET Core best practice for Aspire-hosted services.
- The CORS fix follows SignalR's documented requirement for credential-aware origin policies.
