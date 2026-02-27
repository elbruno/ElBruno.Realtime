namespace Scenario04.Shared.Models;

public record GameStateDto
{
    public float PlayerX { get; init; }
    public float PlayerY { get; init; }
    public string State { get; init; } = "Running"; // Running, Jumping, Falling, Shooting, Dead
    public int Score { get; init; }
    public int Lives { get; init; }
    public float ScrollSpeed { get; init; }
    public bool IsGameOver { get; init; }
}
