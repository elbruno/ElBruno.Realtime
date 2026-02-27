using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using Scenario04.Api.Services;
using Scenario04.Shared.Models;

namespace Scenario04.Api.Hubs;

public sealed class GameHub : Hub
{
    private readonly GameFeedbackService _feedback;
    private readonly IChatClient _chatClient;

    public GameHub(GameFeedbackService feedback, IChatClient chatClient)
    {
        _feedback = feedback;
        _chatClient = chatClient;
    }

    public Task<string> GetFeedback(GameEventDto gameEvent)
    {
        return Task.FromResult(_feedback.GetQuickFeedback(gameEvent.EventType));
    }

    public Task<string> GetMilestoneFeedback(GameEventDto gameEvent)
    {
        return _feedback.GetDynamicFeedbackAsync(gameEvent, Context.ConnectionAborted);
    }

    public async Task<string> ClassifyVoiceCommand(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return "unknown";
        }

        var messages = new[]
        {
            new ChatMessage(ChatRole.System,
                "You are a voice command classifier for a side-scroller game. Respond with one word: jump, shoot, or unknown."),
            new ChatMessage(ChatRole.User, transcript)
        };

        var response = await _chatClient.GetResponseAsync(messages);
        var normalized = (response.Text ?? string.Empty).Trim().ToLowerInvariant();
        var token = normalized.Split(new[] { ' ', '\n', '\r', '\t', '.', '!', '?', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? string.Empty;

        return token switch
        {
            "jump" => "jump",
            "shoot" => "shoot",
            _ => "unknown"
        };
    }
}
