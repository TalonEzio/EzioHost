using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace EzioHost.WebAPI.Hubs;

public class ReverseProxyUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        var id = connection.User.UserId;
        return id.ToString();
    }
}