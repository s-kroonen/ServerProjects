using ChatRoom.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class ChatHub : Hub
{
    #region messages
    private static readonly List<Message> _messages = new();

    public async Task SendMessage(string user, string content)
    {
        var message = new Message { User = user, Content = content };
        _messages.Add(message);
        await Clients.All.SendAsync("ReceiveMessage", message);
    }

    public Task<List<Message>>GetMessageHistory()
    {
        return Task.FromResult(_messages);
    }
    #endregion

    private static int LightBrightness = 100; // default full brightness
    private static string CurrentController = null;
    public Task<string> GetCurrentController() => Task.FromResult(CurrentController);

    public Task<int> GetBrightness() => Task.FromResult(LightBrightness);

    public async Task SetBrightness(string username, int brightness)
    {
        if (CurrentController == null || CurrentController == username)
        {
            LightBrightness = Math.Clamp(brightness, 0, 100);
            await Clients.All.SendAsync("BrightnessChanged", LightBrightness, username);
        }
    }

}
