using EzioHost.Shared.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace EzioHost.Domain.Entities
{
    [Table("Videos")]
    public class Video : BaseAuditableEntityWithUserId<Guid>
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string RawLocation { get; set; } = string.Empty;
        public string M3U8Location { get; set; } = string.Empty;
        public VideoEnum.VideoResolution Resolution { get; set; }
        public VideoEnum.VideoStatus Status { get; set; }
        public VideoEnum.VideoShareType ShareType { get; set; } = VideoEnum.VideoShareType.Private;
        public ICollection<VideoStream> VideoStreams { get; set; } = [];

        public ICollection<VideoUpscale> VideoUpscales { get; set; } = [];

        /// <summary>
        /// Sets Resolution based on video height using tolerance calculation
        /// </summary>
        [NotMapped]
        public int Height
        {
            set => UpdateResolution(value);
        }

        private void UpdateResolution(int height)
        {
            var resolutions = Enum.GetValues<VideoEnum.VideoResolution>()
                .Select(r => new { Resolution = r, Value = (int)r })
                .Where(r => r.Value > 0)
                .OrderBy(r => Math.Abs(height - r.Value))
                .ToList();

            if (resolutions.Any())
            {
                var nearest = resolutions.First();
                var tolerance = nearest.Value * 0.1;
                if (Math.Abs(height - nearest.Value) <= tolerance)
                {
                    Resolution = nearest.Resolution;
                    return;
                }
            }
            Resolution = VideoEnum.VideoResolution._720p;
        }
    }


}
