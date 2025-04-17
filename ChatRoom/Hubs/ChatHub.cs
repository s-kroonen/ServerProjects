using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class ChatHub : Hub
{
    private static readonly List<(string User, string Text)> _messages = new();

    public async Task SendMessage(string user, string message)
    {
        _messages.Add((user, message));
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public Task<List<(string User, string Text)>> GetMessageHistory()
    {
        return Task.FromResult(_messages.ToList());
    }
}
