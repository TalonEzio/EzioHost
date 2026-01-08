using EzioHost.Shared.Events;

namespace EzioHost.Shared.HubActions;

public interface IVideoHubAction
{
    Task ReceiveMessage(string message);
    Task ReceiveNewVideoStream(VideoStreamAddedEventArgs videoChangedEventArgs);
    Task ReceiveVideoProcessingDone(VideoProcessDoneEvent videoProcessDoneEvent);
}