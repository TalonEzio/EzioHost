using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;

namespace EzioHost.UnitTests.TestHelpers;

public static class TestDataBuilder
{
    public static Video CreateVideo(
        Guid? id = null,
        string? title = null,
        Guid? userId = null,
        VideoEnum.VideoStatus status = VideoEnum.VideoStatus.Queue,
        VideoEnum.VideoResolution resolution = VideoEnum.VideoResolution._480p,
        VideoEnum.VideoShareType shareType = VideoEnum.VideoShareType.Private,
        VideoEnum.VideoBackupStatus backupStatus = VideoEnum.VideoBackupStatus.NotBackedUp)
    {
        return new Video
        {
            Id = id ?? Guid.NewGuid(),
            Title = title ?? "Test Video",
            RawLocation = "videos/test-video.mp4",
            M3U8Location = "videos/test-video.m3u8",
            Thumbnail = "thumbnails/test-video.jpg",
            Resolution = resolution,
            Status = status,
            ShareType = shareType,
            BackupStatus = backupStatus,
            CreatedBy = userId ?? Guid.NewGuid(),
            ModifiedBy = userId ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
    }

    public static User CreateUser(
        Guid? id = null,
        string? userName = null,
        string? email = null)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            UserName = userName ?? "testuser",
            FirstName = "Test",
            LastName = "User",
            Email = email ?? "test@example.com",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow
        };
    }

    public static VideoStream CreateVideoStream(
        Guid? id = null,
        Guid? videoId = null,
        VideoEnum.VideoResolution resolution = VideoEnum.VideoResolution._480p)
    {
        return new VideoStream
        {
            Id = id ?? Guid.NewGuid(),
            VideoId = videoId ?? Guid.NewGuid(),
            Resolution = resolution,
            M3U8Location = $"streams/{resolution}/stream.m3u8",
            Key = "test-key-12345678901234567890123456789012",
            Iv = "test-iv-1234567890123456"
        };
    }

    public static VideoSubtitle CreateVideoSubtitle(
        Guid? id = null,
        Guid? videoId = null,
        Guid? userId = null,
        string? language = null)
    {
        return new VideoSubtitle
        {
            Id = id ?? Guid.NewGuid(),
            VideoId = videoId ?? Guid.NewGuid(),
            Language = language ?? "en",
            LocalPath = $"subtitles/{language}/subtitle.vtt",
            CloudPath = $"cloud/subtitles/{language}/subtitle.vtt",
            FileName = "subtitle.vtt",
            FileSize = 1024,
            CreatedBy = userId ?? Guid.NewGuid(),
            ModifiedBy = userId ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
    }

    public static VideoUpscale CreateVideoUpscale(
        Guid? id = null,
        Guid? videoId = null,
        Guid? userId = null,
        Guid? modelId = null,
        VideoEnum.VideoUpscaleStatus status = VideoEnum.VideoUpscaleStatus.Queue)
    {
        return new VideoUpscale
        {
            Id = id ?? Guid.NewGuid(),
            VideoId = videoId ?? Guid.NewGuid(),
            ModelId = modelId ?? Guid.NewGuid(),
            Status = status,
            Scale = 2,
            Resolution = VideoEnum.VideoResolution._480p,
            OutputLocation = "upscaled/output.mp4",
            CreatedBy = userId ?? Guid.NewGuid(),
            ModifiedBy = userId ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
    }

    public static OnnxModel CreateOnnxModel(
        Guid? id = null,
        Guid? userId = null,
        string? name = null)
    {
        return new OnnxModel
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? "test-model",
            FileLocation = "models/test-model.onnx",
            DemoInput = "demo/input.jpg",
            DemoOutput = "demo/output.jpg",
            Scale = 2,
            MustInputWidth = 256,
            MustInputHeight = 256,
            ElementType = TensorElementType.Float,
            CreatedBy = userId ?? Guid.NewGuid(),
            ModifiedBy = userId ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
    }
}
