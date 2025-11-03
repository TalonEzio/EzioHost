namespace EzioHost.Domain.Settings
{
    public class VideoEncodeSetting
    {
        public string VideoCodec { get; set; } = "h264_nvenc";
        public string AudioCodec { get; set; } = "aac";
        public int HlsTime { get; set; } = 1;
        public string BaseDrmUrl { get; set; } = "/api/video/drm/";
    }
}
