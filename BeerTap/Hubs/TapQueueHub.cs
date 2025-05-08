namespace BeerTap.Hubs
{
    using Microsoft.AspNetCore.SignalR;

    public class TapQueueHub : Hub
    {
        public async Task JoinTapGroup(string tapId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, tapId);
        }

        public async Task LeaveTapGroup(string tapId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, tapId);
        }
    }

}
