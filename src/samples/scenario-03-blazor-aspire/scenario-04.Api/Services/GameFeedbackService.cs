using System.Security.Cryptography;
using Microsoft.Extensions.AI;
using Scenario04.Shared.Models;

namespace Scenario04.Api.Services;

public sealed class GameFeedbackService
{
    private static readonly IReadOnlyDictionary<string, string[]> QuickPhrases =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["JumpSuccess"] = ["Nice jump!", "Great dodge!", "Smooth!", "Nailed it!", "Perfect!"],
            ["EnemyKilled"] = ["Got 'em!", "Bullseye!", "Take that!", "Down!", "Boom!"],
            ["VoiceCommand"] = ["Voice power!", "Hands-free!", "Nice call!"],
            ["Death"] = ["Oops!", "Try again!", "Almost had it!", "Watch out next time!"]
        };

    private readonly IChatClient _chatClient;

    public GameFeedbackService(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public string GetQuickFeedback(string eventType)
    {
        var phrases = GetQuickFeedbackPhrases(eventType);
        if (phrases.Length == 0)
        {
            return string.Empty;
        }

        var index = RandomNumberGenerator.GetInt32(phrases.Length);
        return phrases[index];
    }

    public string[] GetQuickFeedbackPhrases(string eventType)
    {
        return QuickPhrases.TryGetValue(eventType, out var phrases)
            ? phrases
            : Array.Empty<string>();
    }

    public async Task<string> GetDynamicFeedbackAsync(GameEventDto gameEvent, CancellationToken ct)
    {
        if (gameEvent.Score <= 0 || gameEvent.Score % 500 != 0)
        {
            return string.Empty;
        }

        var eventDescription = string.IsNullOrWhiteSpace(gameEvent.Detail)
            ? gameEvent.EventType
            : gameEvent.Detail;

        var messages = new[]
        {
            new ChatMessage(ChatRole.System, "You are an enthusiastic game announcer."),
            new ChatMessage(ChatRole.User,
                $"The player just {eventDescription}. Their score is {gameEvent.Score}. Give a short 1-sentence enthusiastic response.")
        };

        var response = await _chatClient.GetResponseAsync(messages);
        return response.Text?.Trim() ?? string.Empty;
    }
}
