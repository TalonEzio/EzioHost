namespace EzioHost.Shared.Hubs
{
    public interface IVideoHubAction
    {
        Task ReceiveMessage(string message);
    }
}
