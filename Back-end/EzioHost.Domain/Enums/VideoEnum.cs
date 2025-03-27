using System.ComponentModel;

namespace EzioHost.Domain.Enums
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

            [Description("Nothing")]
            Nothing = 0,
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

        public enum VideoType : byte
        {
            [Description("Anime / Hoạt hình Nhật Bản")]
            Anime,

            [Description("Phim điện ảnh")]
            Movie,

            [Description("Phim truyền hình")]
            TvShow,

            [Description("Phim tài liệu")]
            Documentary,

            [Description("MV ca nhạc")]
            MusicVideo,

            [Description("Phim ngắn")]
            ShortFilm,

            [Description("Video thể thao")]
            Sports,

            [Description("Nội dung gaming (stream, review)")]
            Gaming,

            [Description("Hướng dẫn, giáo dục")]
            Tutorial,

            [Description("Phát trực tiếp")]
            LiveStream,

            [Description("Tin tức, thời sự")]
            News,

            [Description("Loại khác")]
            Other
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

    }
}
