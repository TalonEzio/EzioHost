using System.ComponentModel;

namespace EzioHost.Shared.Enums
{
    public class VideoEnum
    {
        public enum VideoResolution
        {
            [Description("360p")]
            _360p = 360,

            [Description("480p")]
            _480p = 480,

            [Description("720p")]
            _720p = 720,

            [Description("1080p")]
            _1080p = 1080,

            [Description("1440p")]
            _1440p = 1440,

            [Description("2160p")]
            _2160p = 2160,

            //[Description("Nothing")]
            //Nothing = 0,
        }

        public enum VideoStatus : byte
        {
            [Description("Chờ xử lý")]
            Queue,

            [Description("Đang mã hóa")]
            Encoding,

            [Description("Sẵn sàng")]
            Ready,

            [Description("Đã xóa")]
            Deleted
        }

        [Flags]
        public enum VideoType : ushort
        {
            None = 0,

            [Description("Anime / Hoạt hình Nhật Bản")]
            Anime = 1 << 0, // 1

            [Description("Phim điện ảnh")]
            Movie = 1 << 1, // 2

            [Description("Phim truyền hình")]
            TvShow = 1 << 2, // 4

            [Description("Phim tài liệu")]
            Documentary = 1 << 3, // 8

            [Description("MV ca nhạc")]
            MusicVideo = 1 << 4, // 16

            [Description("Phim ngắn")]
            ShortFilm = 1 << 5, // 32

            [Description("Video thể thao")]
            Sports = 1 << 6, // 64

            [Description("Nội dung gaming (stream, review)")]
            Gaming = 1 << 7, // 128

            [Description("Hướng dẫn, giáo dục")]
            Tutorial = 1 << 8, // 256

            [Description("Phát trực tiếp")]
            LiveStream = 1 << 9, // 512

            [Description("Tin tức, thời sự")]
            News = 1 << 10, // 1024

            [Description("Loại khác")]
            Other = 1 << 11 // 2048
        }
        public enum VideoShareType : byte
        {

            [Description("Riêng tư")]
            Private,
            [Description("Nội bộ (đăng nhập mới xem được)")]
            Internal,
            [Description("Công khai")]
            Public,
        }

        public enum FileUploadStatus : byte
        {
            [Description("Chờ xử lý")]
            Pending,

            [Description("Đang tải lên")]
            InProgress,

            [Description("Hoàn tất")]
            Completed,

            [Description("Thất bại")]
            Failed,

            [Description("Đã hủy")]
            Canceled
        }
    }
}
