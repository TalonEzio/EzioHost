using System.Text;
using System.Text.RegularExpressions;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;

namespace EzioHost.Core.Services.Implement;

public class VideoSubtitleService(
    IVideoSubtitleRepository videoSubtitleRepository,
    IVideoRepository videoRepository,
    IDirectoryProvider directoryProvider,
    IStorageService storageService) : IVideoSubtitleService
{
    private const long MAX_SUBTITLE_FILE_SIZE = 5 * 1024 * 1024; // 5MB
    private const string VTT_EXTENSION = ".vtt";
    private readonly string _webRootPath = directoryProvider.GetWebRootPath();
    private readonly string _baseVideoFolder = directoryProvider.GetBaseVideoFolder();

    public async Task<VideoSubtitle> UploadSubtitleAsync(Guid videoId, string language, Stream fileStream, string fileName, long fileSize, Guid userId)
    {
        // Validate file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension != VTT_EXTENSION)
        {
            throw new ArgumentException($"Chỉ chấp nhận file {VTT_EXTENSION}", nameof(fileName));
        }

        // Validate file size
        if (fileSize > MAX_SUBTITLE_FILE_SIZE)
        {
            throw new ArgumentException($"Kích thước file không được vượt quá {MAX_SUBTITLE_FILE_SIZE / 1024 / 1024}MB", nameof(fileSize));
        }

        // Validate language name (only letters, numbers, spaces)
        if (!Regex.IsMatch(language, @"^[a-zA-Z0-9\s]+$"))
        {
            throw new ArgumentException("Tên ngôn ngữ chỉ được chứa chữ cái, số và khoảng trắng", nameof(language));
        }

        // Check if video exists
        var video = await videoRepository.GetVideoById(videoId);
        if (video == null)
        {
            throw new ArgumentException("Video không tồn tại", nameof(videoId));
        }

        // Validate WebVTT format
        await ValidateWebVttFormatAsync(fileStream);
        fileStream.Position = 0; // Reset stream position

        // Create subtitle directory
        var subtitleDirectory = Path.Combine(_baseVideoFolder, videoId.ToString(), "subtitles");
        if (!Directory.Exists(subtitleDirectory))
        {
            Directory.CreateDirectory(subtitleDirectory);
        }

        // Generate unique file name
        var uniqueFileName = $"{Guid.NewGuid()}{VTT_EXTENSION}";
        var localFilePath = Path.Combine(subtitleDirectory, uniqueFileName);

        // Save to local
        await using (var fileStreamLocal = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(fileStreamLocal);
        }

        var relativeLocalPath = Path.GetRelativePath(_webRootPath, localFilePath);

        // Upload to R2
        var r2Key = $"videos/{videoId}/subtitles/{uniqueFileName}";
        var cloudPath = await storageService.UploadFileAsync(localFilePath, r2Key, "text/vtt");

        // Create entity - only set VideoId, not navigation property
        // EF Core will handle the foreign key relationship automatically
        var subtitle = new VideoSubtitle
        {
            Id = Guid.NewGuid(),
            Language = language.Trim(),
            LocalPath = relativeLocalPath,
            CloudPath = cloudPath,
            FileName = fileName,
            FileSize = fileSize,
            Video = video,
            CreatedBy = userId,
            ModifiedBy = userId
        };

        var savedSubtitle = await videoSubtitleRepository.AddSubtitle(subtitle);

        // Verify VideoId was saved correctly
        if (savedSubtitle.VideoId != videoId)
        {
            throw new InvalidOperationException($"VideoId mismatch: expected {videoId}, got {savedSubtitle.VideoId}");
        }

        return savedSubtitle;
    }

    public Task<IEnumerable<VideoSubtitle>> GetSubtitlesByVideoIdAsync(Guid videoId)
    {
        return videoSubtitleRepository.GetSubtitlesByVideoId(videoId);
    }

    public Task<VideoSubtitle?> GetSubtitleByIdAsync(Guid subtitleId)
    {
        return videoSubtitleRepository.GetSubtitleById(subtitleId);
    }

    public async Task DeleteSubtitleAsync(Guid subtitleId)
    {
        var subtitle = await videoSubtitleRepository.GetSubtitleById(subtitleId);
        if (subtitle == null)
        {
            throw new ArgumentException("Subtitle không tồn tại", nameof(subtitleId));
        }

        // Delete local file
        var localFilePath = Path.Combine(_webRootPath, subtitle.LocalPath);
        if (File.Exists(localFilePath))
        {
            File.Delete(localFilePath);
        }

        // Delete from database
        await videoSubtitleRepository.DeleteSubtitle(subtitle);
    }

    public async Task<Stream> GetSubtitleFileStreamAsync(Guid subtitleId)
    {
        var subtitle = await videoSubtitleRepository.GetSubtitleById(subtitleId);
        if (subtitle == null)
        {
            throw new ArgumentException("Subtitle không tồn tại", nameof(subtitleId));
        }

        var localFilePath = Path.Combine(_webRootPath, subtitle.LocalPath);
        if (!File.Exists(localFilePath))
        {
            throw new FileNotFoundException("File subtitle không tồn tại trên server");
        }

        return new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    private static async Task ValidateWebVttFormatAsync(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var firstLine = await reader.ReadLineAsync();
        
        if (string.IsNullOrWhiteSpace(firstLine) || !firstLine.Trim().StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("File không phải định dạng WebVTT hợp lệ. File phải bắt đầu với 'WEBVTT'");
        }
    }
}
