using EzioHost.Shared.Models;

namespace EzioHost.Shared.Events
{
    public class VideoChangedEvent
    {
        public VideoChangedType ChangedType { get; set; }
        public required VideoDto Video { get; set; }
    }

    public enum VideoChangedType : byte
    {
        Added,
        Edited,
        Deleted,
        EncodingSuccess,
    }
}
