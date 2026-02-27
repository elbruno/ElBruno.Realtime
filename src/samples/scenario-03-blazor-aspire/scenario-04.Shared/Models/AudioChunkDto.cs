namespace Scenario04.Shared.Models;

/// <summary>
/// Audio data chunk sent between browser and server via SignalR.
/// </summary>
public record AudioChunkDto(byte[] Data, int SampleRate = 24000, int Channels = 1);
