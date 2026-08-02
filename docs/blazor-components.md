# Blazor Components — `ElBruno.Realtime.BlazorComponents`

This package provides five ready-to-use Blazor Razor components for building real-time audio conversation UIs on top of the `ElBruno.Realtime` pluggable pipeline.

---

## Installation

```bash
dotnet add package ElBruno.Realtime.BlazorComponents
```

### NuGet badge

[![NuGet](https://img.shields.io/nuget/v/ElBruno.Realtime.BlazorComponents.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.Realtime.BlazorComponents)

---

## DI Registration

Call `AddRealtimeBlazorComponents()` after `AddPersonaPlexRealtime()` in `Program.cs`:

```csharp
builder.Services.AddPersonaPlexRealtime(opts =>
{
    opts.DefaultSystemPrompt = "You are a helpful voice assistant.";
})
.UseWhisperStt("whisper-tiny.en")
.UseSileroVad();

builder.Services.AddRealtimeBlazorComponents(); // ← add this
```

---

## Component Reference

### `ConversationTimeline`

Scrolling turn-by-turn transcript with user and assistant labels. Each turn shows text, and optionally an audio playback indicator when audio is available.

```razor
<ConversationTimeline Turns="@_turns"
                      MaxTurns="100"
                      ShowAudioPlayback="true"
                      MaxHeightPx="400" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Turns` | `IList<ConversationTurn>?` | `null` | The list of conversation turns to display |
| `MaxTurns` | `int` | `100` | Maximum recent turns to display |
| `ShowAudioPlayback` | `bool` | `true` | Show audio indicator when `ResponseAudio` is present |
| `MaxHeightPx` | `int` | `400` | Max height in pixels before the timeline scrolls |
| `CssClass` | `string?` | `null` | Additional CSS class for the container |

---

### `RealtimeSessionPanel`

Full session lifecycle card with Start / Stop / Mute buttons, current VAD state display, and an active-speaker indicator dot.

```razor
<RealtimeSessionPanel IsRunning="@_isRunning"
                      IsVadActive="@_isVadActive"
                      IsMuted="@_isMuted"
                      OnStarted="@HandleStart"
                      OnStopped="@HandleStop"
                      OnMuteToggled="@HandleMute" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `IsRunning` | `bool` | `false` | Whether the session is actively listening |
| `IsVadActive` | `bool` | `false` | Whether VAD has detected speech |
| `IsMuted` | `bool` | `false` | Whether the mic is muted |
| `StartDisabled` | `bool` | `false` | Disable the Start button |
| `OnStarted` | `EventCallback` | — | Fired when Start is clicked |
| `OnStopped` | `EventCallback` | — | Fired when Stop is clicked |
| `OnMuteToggled` | `EventCallback<bool>` | — | Fired when Mute/Unmute is clicked (new muted state) |
| `CssClass` | `string?` | `null` | Additional CSS class |

---

### `AudioLevelMeter`

A CSS-based VU meter bar bound to a 0.0–1.0 `Level` value. Bind it to a field you update from your audio stream or VAD event.

```razor
<AudioLevelMeter Source="AudioSource.Microphone"
                 Level="@_micLevel"
                 Height="40"
                 Color="#0d6efd" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Source` | `AudioSource` | `Microphone` | Which source label to show (`Microphone` or `Speaker`) |
| `Level` | `double` | `0.0` | Current audio level, 0.0–1.0 |
| `Height` | `int` | `40` | Height of the meter in pixels |
| `Color` | `string` | `#0d6efd` | Fill color (any valid CSS color) |
| `ShowLabel` | `bool` | `true` | Show the 🎙/🔊 emoji label |
| `CssClass` | `string?` | `null` | Additional CSS class |

**`AudioSource` enum values:** `Microphone`, `Speaker`

---

### `SessionControls`

Minimal start/stop/mute button cluster — small enough to embed inside any existing page layout.

```razor
<SessionControls IsRunning="@_isRunning"
                 IsMuted="@_isMuted"
                 Size="ControlSize.Small"
                 OnStarted="@HandleStart"
                 OnStopped="@HandleStop"
                 OnMuteToggled="@HandleMute" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `IsRunning` | `bool` | `false` | Whether the session is running |
| `IsMuted` | `bool` | `false` | Whether the mic is muted |
| `Size` | `ControlSize` | `Medium` | Display size of the cluster |
| `Disabled` | `bool` | `false` | Disable all controls |
| `OnStarted` | `EventCallback` | — | Fired when Start is clicked |
| `OnStopped` | `EventCallback` | — | Fired when Stop is clicked |
| `OnMuteToggled` | `EventCallback<bool>` | — | Fired when Mute/Unmute is clicked |
| `CssClass` | `string?` | `null` | Additional CSS class |

**`ControlSize` enum values:** `Small`, `Medium`, `Large`

---

### `LatencyMonitor`

Displays rolling STT / LLM / TTS latency statistics as mini badges with colour coding.

- 🟢 Green — < 300ms
- 🟡 Yellow — 300ms – 799ms
- 🔴 Red — ≥ 800ms

```razor
<LatencyMonitor SttLatencyMs="@_sttMs"
                LlmLatencyMs="@_llmMs"
                TtsLatencyMs="@_ttsMs"
                ShowAverage="true" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `SttLatencyMs` | `double?` | `null` | Most recent STT latency (ms) |
| `LlmLatencyMs` | `double?` | `null` | Most recent LLM first-token latency (ms) |
| `TtsLatencyMs` | `double?` | `null` | Most recent TTS latency (ms) |
| `ShowAverage` | `bool` | `true` | Show an average badge across all three |
| `ShowLabel` | `bool` | `true` | Show the "Latency" title label |
| `CssClass` | `string?` | `null` | Additional CSS class |

---

## Full Working Example

```csharp
// Program.cs
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddPersonaPlexRealtime(opts =>
    opts.DefaultSystemPrompt = "You are a helpful assistant.")
    .UseWhisperStt("whisper-tiny.en")
    .UseSileroVad();

builder.Services.AddChatClient(
    new OllamaChatClient(new Uri("http://localhost:11434"), "phi4-mini"));

builder.Services.AddRealtimeBlazorComponents();
```

```razor
@* Pages/Index.razor *@
@page "/"
@rendermode InteractiveServer
@using ElBruno.Realtime.BlazorComponents
@using ElBruno.Realtime
@inject IRealtimeConversationClient Conversation

<RealtimeSessionPanel IsRunning="@_running"
                      IsVadActive="@_vad"
                      OnStarted="StartSession"
                      OnStopped="StopSession" />

<AudioLevelMeter Source="AudioSource.Microphone" Level="@_level" />

<LatencyMonitor SttLatencyMs="@_sttMs" LlmLatencyMs="@_llmMs" TtsLatencyMs="@_ttsMs" />

<ConversationTimeline Turns="@_turns" />

@code {
    bool _running, _vad;
    double _level;
    double? _sttMs, _llmMs, _ttsMs;
    List<ConversationTurn> _turns = [];

    void StartSession() => _running = true;
    void StopSession() => _running = false;
}
```

---

## See Also

- [Architecture overview](realtime-architecture.md)
- [scenario-05 sample](../src/samples/scenario-05-blazor-components/)
- [ElBruno.Realtime on NuGet](https://www.nuget.org/packages/ElBruno.Realtime)
