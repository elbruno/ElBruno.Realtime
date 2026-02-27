namespace Scenario04.Shared.Models;

public record GameEventDto
{
    public string EventType { get; init; } = ""; // JumpSuccess, EnemyKilled, VoiceCommand, Milestone, Death, GameOver
    public int Score { get; init; }
    public string? Detail { get; init; }
}
