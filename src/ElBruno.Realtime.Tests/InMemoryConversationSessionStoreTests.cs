using Microsoft.Extensions.AI;
using ElBruno.Realtime.Pipeline;

namespace ElBruno.Realtime.Tests;

/// <summary>Tests for InMemoryConversationSessionStore.</summary>
public class InMemoryConversationSessionStoreTests
{
    [Fact]
    public async Task GetOrCreateSessionAsync_ReturnsSameList_ForSameSessionId()
    {
        var store = new InMemoryConversationSessionStore();

        var history1 = await store.GetOrCreateSessionAsync("session-1");
        history1.Add(new ChatMessage(ChatRole.User, "hello"));
        var history2 = await store.GetOrCreateSessionAsync("session-1");

        Assert.Same(history1, history2);
        Assert.Single(history2);
        Assert.Equal("hello", history2[0].Text);
    }

    [Fact]
    public async Task GetOrCreateSessionAsync_ReturnsDifferentLists_ForDifferentSessionIds()
    {
        var store = new InMemoryConversationSessionStore();

        var historyA = await store.GetOrCreateSessionAsync("session-a");
        var historyB = await store.GetOrCreateSessionAsync("session-b");

        historyA.Add(new ChatMessage(ChatRole.User, "from A"));

        Assert.NotSame(historyA, historyB);
        Assert.Single(historyA);
        Assert.Empty(historyB);
    }

    [Fact]
    public async Task RemoveSessionAsync_RemovesSession()
    {
        var store = new InMemoryConversationSessionStore();

        var history = await store.GetOrCreateSessionAsync("session-x");
        history.Add(new ChatMessage(ChatRole.User, "data"));

        await store.RemoveSessionAsync("session-x");

        var newHistory = await store.GetOrCreateSessionAsync("session-x");

        Assert.NotSame(history, newHistory);
        Assert.Empty(newHistory);
    }

    [Fact]
    public async Task RemoveSessionAsync_NonexistentSession_DoesNotThrow()
    {
        var store = new InMemoryConversationSessionStore();

        await store.RemoveSessionAsync("nonexistent");
    }

    [Fact]
    public async Task GetOrCreateSessionAsync_ReturnsEmptyList_ForNewSession()
    {
        var store = new InMemoryConversationSessionStore();

        var history = await store.GetOrCreateSessionAsync("fresh-session");

        Assert.NotNull(history);
        Assert.Empty(history);
    }
}
