using System.ComponentModel.DataAnnotations.Schema;
using EzioHost.Domain.Helpers;
using EzioHost.Shared.Enums;

namespace EzioHost.Domain.Entities;

[Table("Videos")]
public class Video : BaseAuditableEntityWithUserId<Guid>
{
    public Guid Id { get; set; }
    public string Title { get; set => field = Path.GetFileNameWithoutExtension(value); } = string.Empty;

    public string RawLocation
    {
        get;
        set => field = UriPathHelper.NormalizeUriPath(value, nameof(RawLocation));
    } = string.Empty;

    public string M3U8Location
    {
        get;
        set => field = UriPathHelper.NormalizeUriPath(value, nameof(M3U8Location));
    } = string.Empty;

    public string Thumbnail
    {
        get;
        set => field = UriPathHelper.NormalizeUriPath(value, nameof(Thumbnail));
    } = string.Empty;

    public VideoEnum.VideoResolution Resolution { get; set; }
    public VideoEnum.VideoStatus Status { get; set; }
    public VideoEnum.VideoShareType ShareType { get; set; } = VideoEnum.VideoShareType.Private;
    public ICollection<VideoStream> VideoStreams { get; set; } = [];

    public ICollection<VideoUpscale> VideoUpscales { get; set; } = [];

    /// <summary>
    ///     Sets Resolution based on video height using tolerance calculation
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