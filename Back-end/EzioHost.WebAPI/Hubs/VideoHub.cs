using EzioHost.Shared.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EzioHost.WebAPI.Hubs
{
    public class VideoHub : Hub<IVideoHubAction>
    {
        public async Task SendMessage()
        {
            await Clients.All.ReceiveMessage("Test");
        }

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"Connected :{Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Disconnected :{Context.ConnectionId} - {exception?.Message}");

            return base.OnDisconnectedAsync(exception);
        }
    }
}
