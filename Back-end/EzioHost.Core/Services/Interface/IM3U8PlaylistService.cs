using EzioHost.Domain.Entities;

namespace EzioHost.Core.Services.Interface;

public interface IM3U8PlaylistService
{
    /// <summary>
    ///     Builds a complete M3U8 playlist from all video streams
    /// </summary>
    Task BuildFullPlaylistAsync(Video video, string absoluteM3U8Location);

    /// <summary>
    ///     Appends a new video stream entry to an existing M3U8 playlist
    ///     LƯU Ý: Phương thức này có thể gây ra trùng lặp nếu không được sử dụng cẩn thận.
    ///     Nên sử dụng BuildFullPlaylistAsync để xây dựng lại toàn bộ playlist từ database.
    /// </summary>
    Task AppendStreamToPlaylistAsync(Video video, VideoStream videoStream, string absoluteM3U8Location);
}