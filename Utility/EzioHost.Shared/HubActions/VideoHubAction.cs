using EzioHost.Shared.Events;
using EzioHost.Shared.Models;

namespace EzioHost.Shared.HubActions;

public interface IVideoHubAction
{
    Task ReceiveMessage(string message);
    Task ReceiveNewVideoStream(VideoStreamAddedEventArgs videoChangedEventArgs);
    Task ReceiveVideoProcessingDone(VideoProcessDoneEvent videoProcessDoneEvent);
    Task ReceiveVideoUpscaleStarted(Guid videoId);
    Task ReceiveSubtitleTranscribed(VideoSubtitleDto subtitle);
}