using EzioHost.Shared.Models;

namespace EzioHost.Shared.Events;

public class VideoStreamAddedEvent
{
    public required Guid VideoId { get; set; }
    public required VideoStreamDto VideoStream { get; set; }
}