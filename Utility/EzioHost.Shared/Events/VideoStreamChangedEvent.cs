using EzioHost.Shared.Models;

namespace EzioHost.Shared.Events;

public class VideoStreamAddedEventArgs : EventArgs
{
    public required Guid VideoId { get; set; }
    public required VideoStreamDto VideoStream { get; set; }
}