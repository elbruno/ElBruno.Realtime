# Decision: Scenario 04 — Real-Time Microphone Console App

**Date:** 2026-02-28
**By:** Lambert (Frontend Developer)
**Status:** ✅ Implemented

## What

Created `src/samples/scenario-04-realtime-console/` — a console app that captures microphone audio and runs a continuous conversation loop: Microphone → Whisper STT → Ollama LLM → Console text output → Loop.

## Key Decisions

1. **NAudio `WaveInEvent`** for mic capture — works on background threads, Windows-focused but .NET 10 compatible.
2. **RMS-based silence detection** — simple threshold approach (speech ≥ 1000, silence ≤ 500, 1.5s duration). No external VAD dependency for mic-level detection.
3. **Manual WAV header** — raw PCM wrapped with RIFF/WAV header since the pipeline expects WAV format.
4. **No TTS** — text-only responses. Users can add QwenTTS or System.Speech later if desired.
5. **Audio format: 16kHz, 16-bit, mono** — matches Whisper pipeline expectations exactly.

## Build

✅ 0 errors, 0 warnings (net10.0)
