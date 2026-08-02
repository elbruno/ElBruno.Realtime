using ElBruno.Realtime;
using ElBruno.Realtime.BlazorComponents;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add the real-time conversation pipeline
// Swap out the STT and TTS providers to match your environment.
// Whisper + SileroVad are shown as defaults; models auto-download on first run.
builder.Services.AddPersonaPlexRealtime(opts =>
{
    opts.DefaultSystemPrompt = "You are a helpful voice assistant.";
});
// .UseWhisperStt("whisper-tiny.en")  // uncomment and configure your STT
// .UseSileroVad();                   // uncomment to enable VAD

// Register BlazorComponents services (no-op today, future-proofed extension point)
builder.Services.AddRealtimeBlazorComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<scenario_05_Web.App>()
   .AddInteractiveServerRenderMode();

app.Run();
