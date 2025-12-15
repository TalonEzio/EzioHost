using EzioHost.Shared.HubActions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EzioHost.WebAPI.Hubs;

[Authorize]
public class VideoHub : Hub<IVideoHubAction>
{
}