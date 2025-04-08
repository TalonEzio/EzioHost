namespace EzioHost.Shared.Hubs
{
    public interface IVideoHubAction
    {
        Task OnConnected(string s);
        Task ReceiveMessage(string message);
    }
}
