using EzioHost.Shared.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EzioHost.WebAPI.Hubs
{
    [Authorize]
    public class VideoHub : Hub<IVideoHubAction>
    {
        public async Task SendMessage()
        {
            var userName = Context.User?.Claims.FirstOrDefault(x => x.Type == "name")?.Value;

            await Clients.Caller.ReceiveMessage($"Xin chào {userName}");
        }
    }
}
