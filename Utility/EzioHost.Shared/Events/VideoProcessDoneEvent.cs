using EzioHost.Shared.Models;

namespace EzioHost.Shared.Events;

public class VideoProcessDoneEvent : EventArgs
{
    public required VideoDto Video { get; set; }
}