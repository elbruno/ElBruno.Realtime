# Issue #3 Scope Decision — GPU Device Selection for TTS

**Decision Date:** 2026-02-27  
**By:** Ripley (Lead / Architect)  
**Issue:** [#3](https://github.com/elbruno/ElBruno.Realtime/issues/3) "Sometimes unclear how to 'useGpu' in TTS with AddPersonaPlexRealtime"

---

## Problem Statement

**User's Pain Point:**  
User has a CUDA-capable GPU but it's not device 0 (the default). When calling `.UseQwenTts()` on `RealtimeBuilder`, there's no obvious way to specify which GPU device to use or pass custom `SessionOptions` for CUDA configuration. The user had to **remove** the TTS call entirely to default to text-only output.

**Current Code Flow (scenario-01-console):**
```csharp
services.AddPersonaPlexRealtime(opts => { ... })
    .UseWhisperStt(whisperModelId)   // ✅ Has useGpu: true parameter (device 0 only)
    .UseQwenTts();                    // ❌ No GPU configuration exposed
```

**What the external QwenTTS library supports ([docs](https://github.com/elbruno/ElBruno.QwenTTS/blob/main/docs/gpu-acceleration.md)):**
```csharp
// Direct TtsPipeline API (from ElBruno.QwenTTS package)
var tts = await TtsPipeline.CreateAsync(
    sessionOptionsFactory: OrtSessionHelper.CreateCudaOptions,  // GPU device 0
    sessionOptionsFactory: () => {
        var opts = new SessionOptions();
        opts.AppendExecutionProvider_CUDA(deviceId: 1);  // GPU device 1 🎯
        return opts;
    });
```

But the **Realtime bridge** (`UseQwenTts()`) **hides** this configuration:
```csharp
// ElBruno.QwenTTS.Realtime/QwenTtsRealtimeExtensions.cs (current)
public static RealtimeBuilder UseQwenTts(this RealtimeBuilder builder)
{
    builder.Services.AddQwenTts();  // ❌ No options passed
    builder.Services.AddSingleton<ITextToSpeechClient, QwenTextToSpeechClientAdapter>();
    return builder;
}
```

The `AddQwenTts()` DI extension **does** accept options configuration ([ElBruno.QwenTTS v0.1.8-preview](https://github.com/elbruno/ElBruno.QwenTTS)):
```csharp
// What the external package supports but we don't expose:
services.AddQwenTts(options => 
{
    options.ExecutionProvider = ExecutionProvider.CUDA;  // or DirectML, CPU
    options.DeviceId = 1;  // ✅ This is what the user needs
});
```

---

## Root Cause

**Gap 1 (Code):** The `UseQwenTts()` extension method has **no parameters** — it's a zero-configuration convenience method that assumes CPU execution on device 0.

**Gap 2 (Docs):** Neither the main README nor the sample READMEs mention GPU acceleration for TTS. The only reference is in `docs/models-overview.md`, which states:
> **Configuration:**  
> ```csharp
> .UseQwenTts()                          // default voice: "ryan"
> .UseQwenTts(defaultVoice: "serena")    // different voice
> .UseQwenTts(defaultLanguage: "english")
> ```
> 
> *(No mention of GPU configuration)*

**Gap 3 (Discoverability):** The external [ElBruno.QwenTTS GPU acceleration docs](https://github.com/elbruno/ElBruno.QwenTTS/blob/main/docs/gpu-acceleration.md) are comprehensive but exist in a **separate repo**. Users of ElBruno.Realtime won't know to look there.

**Asymmetry:** `UseWhisperStt()` **does** expose a `useGpu: true` parameter (though only for device 0), creating an inconsistent API surface.

---

## Proposed Solution

### Code Changes (Minimal API Extension)

**Option A: Add GPU parameters to `UseQwenTts()` signature** (mirrors Whisper pattern)
```csharp
public static RealtimeBuilder UseQwenTts(
    this RealtimeBuilder builder,
    string defaultVoice = "ryan",
    string defaultLanguage = "auto",
    bool useGpu = false,              // 🆕 Enable GPU (defaults to CPU for safety)
    int deviceId = 0)                 // 🆕 GPU device ID
{
    builder.Services.AddQwenTts(options =>
    {
        if (useGpu)
        {
            options.ExecutionProvider = ExecutionProvider.CUDA;  // or detect CUDA vs DirectML?
            options.DeviceId = deviceId;
        }
        else
        {
            options.ExecutionProvider = ExecutionProvider.CPU;
        }
    });
    builder.Services.AddSingleton<ITextToSpeechClient, QwenTextToSpeechClientAdapter>();
    return builder;
}
```

**Pros:**  
- Minimal API change (2 optional parameters)  
- Mirrors existing `UseWhisperStt(useGpu: true)` pattern  
- Solves user's issue directly  

**Cons:**  
- Doesn't expose full `SessionOptions` factory (for advanced use)  
- Assumes CUDA (need to detect or let user choose DirectML vs CUDA?)  

**Option B: Accept `Action<QwenTtsOptions>` callback** (more flexible)
```csharp
public static RealtimeBuilder UseQwenTts(
    this RealtimeBuilder builder,
    Action<QwenTtsOptions>? configure = null)
{
    builder.Services.AddQwenTts(configure);
    builder.Services.AddSingleton<ITextToSpeechClient, QwenTextToSpeechClientAdapter>();
    return builder;
}
```

**Usage:**
```csharp
.UseQwenTts(opts =>
{
    opts.ExecutionProvider = ExecutionProvider.CUDA;
    opts.DeviceId = 1;  // 🎯 User's GPU
})
```

**Pros:**  
- Full flexibility (exposes all `QwenTtsOptions`)  
- Future-proof (any new option is automatically available)  
- Single parameter addition  

**Cons:**  
- Less discoverable than explicit `useGpu, deviceId` parameters  
- Requires users to know `QwenTtsOptions` exists  

**Recommendation:** **Option B** (callback pattern) — balances flexibility and API simplicity. Add IntelliSense XML docs with GPU example.

---

### Documentation Changes

**1. Main README (`README.md`):**  
Add GPU acceleration section after "Quick Start":

````markdown
## GPU Acceleration

### Whisper STT (GPU)
```csharp
.UseWhisperStt("whisper-tiny.en", useGpu: true)  // Device 0 (default GPU)
```

### QwenTTS (GPU)
```csharp
.UseQwenTts(opts =>
{
    opts.ExecutionProvider = ExecutionProvider.CUDA;  // or DirectML for Windows
    opts.DeviceId = 1;  // Select specific GPU (default: 0)
})
```

**Requirements:**
- **CUDA:** Install `Microsoft.ML.OnnxRuntime.Gpu` NuGet (instead of `Microsoft.ML.OnnxRuntime`)  
  + CUDA Toolkit + cuDNN  
- **DirectML (Windows):** Install `Microsoft.ML.OnnxRuntime.DirectML` NuGet

For full details, see [ElBruno.QwenTTS GPU docs](https://github.com/elbruno/ElBruno.QwenTTS/blob/main/docs/gpu-acceleration.md).
````

**2. Sample README updates:**  
- `scenario-01-console/README.md` — add "GPU Acceleration" section  
- `scenario-04-realtime-console/README.md` — same

**3. Models Overview (`docs/models-overview.md`):**  
Expand QwenTTS configuration example to include GPU:

```markdown
**Configuration:**
```csharp
// Voice & language
.UseQwenTts(opts => opts.DefaultVoice = "serena")

// GPU acceleration (requires Microsoft.ML.OnnxRuntime.Gpu)
.UseQwenTts(opts =>
{
    opts.ExecutionProvider = ExecutionProvider.CUDA;
    opts.DeviceId = 1;  // GPU device 1
})
```

**External docs:** Link to [ElBruno.QwenTTS GPU acceleration docs](https://github.com/elbruno/ElBruno.QwenTTS/blob/main/docs/gpu-acceleration.md)
```

---

### Example Code (scenario-01-console)

Update `Program.cs` to show GPU usage in comments:

```csharp
services.AddPersonaPlexRealtime(opts => { ... })
    .UseWhisperStt(whisperModelId, useGpu: true)   // GPU device 0
    .UseQwenTts();                                 // CPU (default)
    // .UseQwenTts(opts =>                         // GPU device 1
    // {
    //     opts.ExecutionProvider = ExecutionProvider.CUDA;
    //     opts.DeviceId = 1;
    // })
```

---

## Estimate

| Task | Type | Effort |
|------|------|--------|
| Update `UseQwenTts()` signature to accept `Action<QwenTtsOptions>?` | Code | 10 min |
| Add XML docs with GPU example | Code | 10 min |
| Update main README GPU section | Docs | 15 min |
| Update scenario-01 README | Docs | 10 min |
| Update scenario-04 README | Docs | 10 min |
| Update models-overview.md | Docs | 10 min |
| Add commented GPU example to scenario-01/Program.cs | Code | 5 min |
| Build + smoke test | Test | 10 min |

**Total:** ~1.5 hours (both code + docs)

**Type:** **Both** — Small code change (API surface extension) + documentation clarification

---

## Next Steps

**Assign to:**
1. **Dallas** (C# Dev) — Implement Option B code changes, update samples
2. **Parker** (DevOps) — Update README files with GPU section
3. **Kane** (Tester) — Smoke test with CUDA GPU (if available) or document CPU-only test

**Verification:**
- User can call `.UseQwenTts(opts => opts.DeviceId = 1)` to select GPU device 1  
- Build passes (0 errors, 0 warnings)  
- Documentation links to external GPU acceleration guide  
- Sample code shows GPU configuration in comments  

---

## Decision

✅ **APPROVED** — Minimal scope, high value. Solves user's issue without over-engineering.

**Breaking change?** No — new optional parameter with default behavior unchanged.

**Dependencies?** None — relies on existing `ElBruno.QwenTTS` v0.1.8-preview API.

**Rollout:** Merge to main → publish NuGet update (patch version bump).
