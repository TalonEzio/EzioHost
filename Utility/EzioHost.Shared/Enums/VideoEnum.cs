using System.ComponentModel;

namespace EzioHost.Shared.Enums;

public class VideoEnum
{
    public enum FileUploadStatus : byte
    {
        [Description("Chờ xử lý")] Pending,

        [Description("Đang tải lên")] InProgress,

        [Description("Hoàn tất")] Completed,

        [Description("Thất bại")] Failed,

        [Description("Đã hủy")] Canceled
    }

    public enum VideoBackupStatus : byte
    {
        [Description("Chưa sao lưu")] NotBackedUp = 0,
        [Description("Đang sao lưu")] BackingUp = 1,
        [Description("Đã sao lưu")] BackedUp = 2,
        [Description("Sao lưu thất bại")] BackupFailed = 3
    }

    public enum VideoResolution
    {
        [Description("144p")] _144p = 144,

        [Description("240p")] _240p = 240,

        [Description("360p")] _360p = 360,

        [Description("480p")] _480p = 480,

        [Description("720p")] _720p = 720,

        [Description("960p")] _960p = 960,

        [Description("1080p")] _1080p = 1080,

        [Description("1440p")] _1440p = 1440,

        [Description("1920p")] _1920p = 1920,

        [Description("2160p")] _2160p = 2160,

        [Description("AI Upscaled")] Upscaled = 3636
    }

    public enum VideoShareType : byte
    {
        [Description("Riêng tư")] Private,

        [Description("Nội bộ (đăng nhập mới xem được)")]
        Internal,
        [Description("Công khai")] Public
    }

    public enum VideoStatus : byte
    {
        [Description("Chờ xử lý")] Queue,

        [Description("Đang mã hóa")] Encoding,

        [Description("Sẵn sàng")] Ready,

        [Description("Đã xóa")] Deleted
    }

    public enum VideoUpscaleStatus : byte
    {
        Queue = 0,
        Ready = 1
    }

    public enum SubtitleTranscribeStatus : byte
    {
        [Description("Chờ xử lý")] Queue = 0,
        [Description("Đang xử lý")] Processing = 1,
        [Description("Hoàn tất")] Completed = 2,
        [Description("Thất bại")] Failed = 3
    }
}