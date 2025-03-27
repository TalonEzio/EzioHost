using EzioHost.Domain.Entities;

namespace EzioHost.Domain.Events
{
    public class VideoChangedEvent
    {
        public VideoChangedType ChangedType { get; set; }
        public required Video Instance { get; set; }
    }

    public enum VideoChangedType : byte
    {
        Added,
        Edited,
        Deleted,
        EncodingSuccess,
    }
}
