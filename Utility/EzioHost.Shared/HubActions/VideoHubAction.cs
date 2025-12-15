using EzioHost.Shared.Events;

namespace EzioHost.Shared.HubActions;

public interface IVideoHubAction
{
    Task ReceiveMessage(string message);
    Task ReceiveNewVideoStream(VideoStreamAddedEvent videoChangedEvent);
    Task ReceiveVideoProcessingDone(VideoProcessDoneEvent videoProcessDoneEvent);
}