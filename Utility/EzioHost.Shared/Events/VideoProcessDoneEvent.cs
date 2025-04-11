using EzioHost.Shared.Models;

namespace EzioHost.Shared.Events
{
    public class VideoProcessDoneEvent
    {
        public required VideoDto Video { get; set; }
    }
}
