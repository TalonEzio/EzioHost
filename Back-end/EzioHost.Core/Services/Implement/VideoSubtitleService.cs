using System.Text;
using System.Text.RegularExpressions;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EzioHost.Core.Services.Implement;

public class VideoSubtitleService(
    IVideoSubtitleRepository videoSubtitleRepository,
    IVideoRepository videoRepository,
    IDirectoryProvider directoryProvider,
    IStorageService storageService,
    ILogger<VideoSubtitleService> logger) : IVideoSubtitleService
{
    private const long MAX_SUBTITLE_FILE_SIZE = 5 * 1024 * 1024; // 5MB
    private const string VTT_EXTENSION = ".vtt";
    private readonly string _baseVideoFolder = directoryProvider.GetBaseVideoFolder();
    private readonly string _webRootPath = directoryProvider.GetWebRootPath();

    public async Task<VideoSubtitle> UploadSubtitleAsync(Guid videoId, string language, Stream fileStream,
        string fileName, long fileSize, Guid userId)
    {
        logger.LogInformation(
            "Uploading subtitle. VideoId: {VideoId}, Language: {Language}, FileName: {FileName}, FileSize: {FileSize} bytes, UserId: {UserId}",
            videoId,
            language,
            fileName,
            fileSize,
            userId);

        // Validate file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension != VTT_EXTENSION)
        {
            logger.LogWarning(
                "Invalid file extension for subtitle upload. VideoId: {VideoId}, FileName: {FileName}, Extension: {Extension}",
                videoId,
                fileName,
                extension);
            throw new ArgumentException($"Chỉ chấp nhận file {VTT_EXTENSION}", nameof(fileName));
        }

        // Validate file size
        if (fileSize > MAX_SUBTITLE_FILE_SIZE)
        {
            logger.LogWarning(
                "Subtitle file size exceeds limit. VideoId: {VideoId}, FileSize: {FileSize} bytes, MaxSize: {MaxSize} bytes",
                videoId,
                fileSize,
                MAX_SUBTITLE_FILE_SIZE);
            throw new ArgumentException($"Kích thước file không được vượt quá {MAX_SUBTITLE_FILE_SIZE / 1024 / 1024}MB",
                nameof(fileSize));
        }

        // Validate language name (only letters, numbers, spaces)
        if (!Regex.IsMatch(language, @"^[a-zA-Z0-9\s]+$"))
        {
            logger.LogWarning(
                "Invalid language name format. VideoId: {VideoId}, Language: {Language}",
                videoId,
                language);
            throw new ArgumentException("Tên ngôn ngữ chỉ được chứa chữ cái, số và khoảng trắng", nameof(language));
        }

        // Check if video exists
        var video = await videoRepository.GetVideoById(videoId);
        if (video == null)
        {
            logger.LogWarning("Video not found for subtitle upload. VideoId: {VideoId}, UserId: {UserId}", videoId, userId);
            throw new ArgumentException("Video không tồn tại", nameof(videoId));
        }

        // Validate WebVTT format
        logger.LogDebug("Validating WebVTT format. VideoId: {VideoId}, FileName: {FileName}", videoId, fileName);
        await ValidateWebVttFormatAsync(fileStream);
        fileStream.Position = 0; // Reset stream position
        logger.LogDebug("WebVTT format validation passed. VideoId: {VideoId}", videoId);

        // Create subtitle directory
        var subtitleDirectory = Path.Combine(_baseVideoFolder, videoId.ToString(), "subtitles");
        if (!Directory.Exists(subtitleDirectory))
        {
            Directory.CreateDirectory(subtitleDirectory);
            logger.LogDebug("Created subtitle directory. VideoId: {VideoId}, Directory: {Directory}", videoId, subtitleDirectory);
        }

        // Generate unique file name
        var uniqueFileName = $"{Guid.NewGuid()}{VTT_EXTENSION}";
        var localFilePath = Path.Combine(subtitleDirectory, uniqueFileName);

        // Save to local
        logger.LogDebug("Saving subtitle to local storage. VideoId: {VideoId}, LocalPath: {LocalPath}", videoId, localFilePath);
        await using (var fileStreamLocal = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(fileStreamLocal);
        }

        var relativeLocalPath = Path.GetRelativePath(_webRootPath, localFilePath);

        // Upload to R2
        var r2Key = $"videos/{videoId}/subtitles/{uniqueFileName}";
        logger.LogDebug("Uploading subtitle to cloud storage. VideoId: {VideoId}, Key: {Key}", videoId, r2Key);
        var cloudPath = await storageService.UploadFileAsync(localFilePath, r2Key, "text/vtt");
        logger.LogDebug("Subtitle uploaded to cloud storage. VideoId: {VideoId}, CloudPath: {CloudPath}", videoId, cloudPath);

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
            logger.LogError(
                "VideoId mismatch after saving subtitle. VideoId: {VideoId}, SavedVideoId: {SavedVideoId}, SubtitleId: {SubtitleId}",
                videoId,
                savedSubtitle.VideoId,
                savedSubtitle.Id);
            throw new InvalidOperationException($"VideoId mismatch: expected {videoId}, got {savedSubtitle.VideoId}");
        }

        logger.LogInformation(
            "Subtitle uploaded successfully. SubtitleId: {SubtitleId}, VideoId: {VideoId}, Language: {Language}",
            savedSubtitle.Id,
            videoId,
            language);

        return savedSubtitle;
    }

    public async Task<IEnumerable<VideoSubtitle>> GetSubtitlesByVideoIdAsync(Guid videoId)
    {
        logger.LogDebug("Getting subtitles by video ID: {VideoId}", videoId);
        var subtitles = await videoSubtitleRepository.GetSubtitlesByVideoId(videoId);
        logger.LogDebug("Retrieved {Count} subtitles for video {VideoId}", subtitles.Count(), videoId);
        return subtitles;
    }

    public Task<VideoSubtitle?> GetSubtitleByIdAsync(Guid subtitleId)
    {
        logger.LogDebug("Getting subtitle by ID: {SubtitleId}", subtitleId);
        return videoSubtitleRepository.GetSubtitleById(subtitleId);
    }

    public async Task DeleteSubtitleAsync(Guid subtitleId)
    {
        logger.LogInformation("Deleting subtitle. SubtitleId: {SubtitleId}", subtitleId);

        var subtitle = await videoSubtitleRepository.GetSubtitleById(subtitleId);
        if (subtitle == null)
        {
            logger.LogWarning("Subtitle not found for deletion. SubtitleId: {SubtitleId}", subtitleId);
            throw new ArgumentException("Subtitle không tồn tại", nameof(subtitleId));
        }

        // Delete local file
        var localFilePath = Path.Combine(_webRootPath, subtitle.LocalPath);
        if (File.Exists(localFilePath))
        {
            File.Delete(localFilePath);
            logger.LogDebug("Deleted local subtitle file. SubtitleId: {SubtitleId}, Path: {Path}", subtitleId, localFilePath);
        }

        // Delete from database
        await videoSubtitleRepository.DeleteSubtitle(subtitle);
        logger.LogInformation("Successfully deleted subtitle {SubtitleId}", subtitleId);
    }

    public async Task<Stream> GetSubtitleFileStreamAsync(Guid subtitleId)
    {
        logger.LogDebug("Getting subtitle file stream. SubtitleId: {SubtitleId}", subtitleId);

        var subtitle = await videoSubtitleRepository.GetSubtitleById(subtitleId);
        if (subtitle == null)
        {
            logger.LogWarning("Subtitle not found. SubtitleId: {SubtitleId}", subtitleId);
            throw new ArgumentException("Subtitle không tồn tại", nameof(subtitleId));
        }

        var localFilePath = Path.Combine(_webRootPath, subtitle.LocalPath);
        if (!File.Exists(localFilePath))
        {
            logger.LogError("Subtitle file not found on server. SubtitleId: {SubtitleId}, Path: {Path}", subtitleId, localFilePath);
            throw new FileNotFoundException("File subtitle không tồn tại trên server");
        }

        logger.LogDebug("Returning subtitle file stream. SubtitleId: {SubtitleId}, Path: {Path}", subtitleId, localFilePath);
        return new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    private static async Task ValidateWebVttFormatAsync(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var firstLine = await reader.ReadLineAsync();

        if (string.IsNullOrWhiteSpace(firstLine) ||
            !firstLine.Trim().StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File không phải định dạng WebVTT hợp lệ. File phải bắt đầu với 'WEBVTT'");
    }
}