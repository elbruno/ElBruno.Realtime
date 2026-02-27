namespace Scenario04.Shared.Models;

public record GameInputDto
{
    public bool Jump { get; init; }
    public bool Shoot { get; init; }
    public string? VoiceTranscript { get; init; }
}
