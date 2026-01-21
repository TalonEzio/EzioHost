using System.Text;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Helpers;
using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.Extensions.Logging;
using Whisper.net;
using Whisper.net.Ggml;

namespace EzioHost.Core.Services.Implement;

public class SubtitleTranscribeService(
    ISubtitleTranscribeRepository subtitleTranscribeRepository,
    IVideoRepository videoRepository,
    IVideoSubtitleService videoSubtitleService,
    ISubtitleTranscribeSettingService subtitleTranscribeSettingService,
    IDirectoryProvider directoryProvider,
    ILogger<SubtitleTranscribeService> logger) : ISubtitleTranscribeService
{
    private readonly string _webRootPath = directoryProvider.GetWebRootPath();
    private readonly string _tempPath = directoryProvider.GetTempPath();

    public async Task<SubtitleTranscribe> CreateTranscribeRequestAsync(Guid videoId, string language, Guid userId)
    {
        logger.LogInformation(
            "Creating transcribe request. VideoId: {VideoId}, Language: {Language}, UserId: {UserId}",
            videoId,
            language,
            userId);

        // Check user settings - IsEnabled
        var userSettings = await subtitleTranscribeSettingService.GetUserSettingsAsync(userId);
        if (!userSettings.IsEnabled)
        {
            logger.LogWarning(
                "Transcribe feature is disabled for user. VideoId: {VideoId}, UserId: {UserId}",
                videoId,
                userId);
            throw new InvalidOperationException("Tính năng Audio Transcribing đã bị tắt. Vui lòng bật trong Settings.");
        }

        // Check if video exists and is ready
        var video = await videoRepository.GetVideoById(videoId);
        if (video == null)
        {
            logger.LogWarning("Video not found for transcribe request. VideoId: {VideoId}, UserId: {UserId}", videoId, userId);
            throw new ArgumentException("Video không tồn tại", nameof(videoId));
        }

        if (video.Status != VideoEnum.VideoStatus.Ready)
        {
            logger.LogWarning(
                "Video is not ready for transcription. VideoId: {VideoId}, Status: {Status}, UserId: {UserId}",
                videoId,
                video.Status,
                userId);
            throw new InvalidOperationException("Video phải ở trạng thái Ready mới có thể transcribe");
        }

        // Check if video already has subtitles
        var existingSubtitles = await videoSubtitleService.GetSubtitlesByVideoIdAsync(videoId);
        if (existingSubtitles.Any())
        {
            logger.LogWarning(
                "Video already has subtitles. VideoId: {VideoId}, ExistingSubtitlesCount: {Count}, UserId: {UserId}",
                videoId,
                existingSubtitles.Count(),
                userId);
            throw new InvalidOperationException(
                "Video đã có subtitle. Vui lòng xóa subtitle cũ trước khi transcribe lại.");
        }

        // Check if there's already a pending/processing transcribe request
        var existingRequest = await subtitleTranscribeRepository.ExistsByVideoIdAsync(videoId);
        if (existingRequest)
        {
            logger.LogWarning(
                "Transcribe request already exists for video. VideoId: {VideoId}, UserId: {UserId}",
                videoId,
                userId);
            throw new InvalidOperationException("Video đã có request transcribe đang chờ xử lý");
        }

        var transcribe = new SubtitleTranscribe
        {
            Id = Guid.NewGuid(),
            VideoId = videoId,
            Language = language,
            Status = VideoEnum.SubtitleTranscribeStatus.Queue,
            Video = video,
            CreatedBy = userId,
            ModifiedBy = userId
        };

        var result = await subtitleTranscribeRepository.AddAsync(transcribe);
        logger.LogInformation(
            "Transcribe request created successfully. SubtitleTranscribeId: {SubtitleTranscribeId}, VideoId: {VideoId}",
            result.Id,
            videoId);

        return result;
    }

    public Task<SubtitleTranscribe?> GetNextTranscribeJobAsync()
    {
        return subtitleTranscribeRepository.GetNextJobAsync();
    }

    public async Task ProcessTranscriptionAsync(SubtitleTranscribe transcribe)
    {
        var overallStopwatch = System.Diagnostics.Stopwatch.StartNew();
        logger.LogInformation(
            "Starting transcription processing. SubtitleTranscribeId: {SubtitleTranscribeId}, VideoId: {VideoId}, Language: {Language}",
            transcribe.Id,
            transcribe.VideoId,
            transcribe.Language);

        try
        {
            // Update status to Processing
            transcribe.Status = VideoEnum.SubtitleTranscribeStatus.Processing;
            await subtitleTranscribeRepository.UpdateAsync(transcribe);
            logger.LogDebug("Updated transcribe status to Processing. SubtitleTranscribeId: {SubtitleTranscribeId}", transcribe.Id);

            var video = transcribe.Video;

            var videoPath = Path.Combine(_webRootPath, video.RawLocation);
            if (!File.Exists(videoPath))
            {
                logger.LogError(
                    "Video file not found for transcription. SubtitleTranscribeId: {SubtitleTranscribeId}, VideoId: {VideoId}, Path: {VideoPath}",
                    transcribe.Id,
                    transcribe.VideoId,
                    videoPath);
                throw new FileNotFoundException($"Video file không tồn tại: {videoPath}");
            }

            // Extract audio from video using FFmpeg
            var audioFileName = $"{Guid.NewGuid()}.wav";
            var audioPath = Path.Combine(_tempPath, audioFileName);

            logger.LogInformation(
                "Extracting audio from video. SubtitleTranscribeId: {SubtitleTranscribeId}, VideoId: {VideoId}",
                transcribe.Id,
                transcribe.VideoId);

            var audioExtractionStopwatch = System.Diagnostics.Stopwatch.StartNew();

            var audioExtractionArguments = FFMpegArguments
                .FromFileInput(videoPath)
                .OutputToFile(audioPath, true, options => options
                    .WithCustomArgument("-f wav")
                    .WithCustomArgument("-acodec pcm_s16le")
                    .WithCustomArgument("-ar 16000") // 16kHz sample rate
                    .WithCustomArgument("-ac 1") // Mono channel
                );

            await audioExtractionArguments.ProcessAsynchronously();

            audioExtractionStopwatch.Stop();

            if (!File.Exists(audioPath))
            {
                logger.LogError(
                    "Audio extraction failed - output file not found. SubtitleTranscribeId: {SubtitleTranscribeId}, VideoId: {VideoId}",
                    transcribe.Id,
                    transcribe.VideoId);
                throw new InvalidOperationException("Không thể extract audio từ video");
            }

            var audioFileInfo = new FileInfo(audioPath);
            logger.LogInformation(
                "Audio extraction completed. SubtitleTranscribeId: {SubtitleTranscribeId}, AudioSize: {AudioSize} bytes, Duration: {DurationMs}ms",
                transcribe.Id,
                audioFileInfo.Length,
                audioExtractionStopwatch.ElapsedMilliseconds);

            try
            {
                // Get user settings for model type and GPU
                var userSettings = await subtitleTranscribeSettingService.GetUserSettingsAsync(transcribe.CreatedBy);

                // Map WhisperModelType to GgmlType
                var ggmlType = userSettings.ModelType switch
                {
                    WhisperEnum.WhisperModelType.Tiny => GgmlType.Tiny,
                    WhisperEnum.WhisperModelType.Base => GgmlType.Base,
                    WhisperEnum.WhisperModelType.Small => GgmlType.Small,
                    WhisperEnum.WhisperModelType.Medium => GgmlType.Medium,
                    WhisperEnum.WhisperModelType.Large => GgmlType.LargeV3,
                    _ => GgmlType.Base
                };

                var modelFileName = $"ggml-{userSettings.ModelType.ToString().ToLowerInvariant()}.bin";
                var modelPath = Path.Combine(_tempPath, modelFileName);

                // Download Whisper model if needed
                if (!File.Exists(modelPath))
                {
                    logger.LogInformation(
                        "Downloading Whisper model. SubtitleTranscribeId: {SubtitleTranscribeId}, ModelType: {ModelType}, Path: {ModelPath}",
                        transcribe.Id,
                        userSettings.ModelType,
                        modelPath);

                    var downloadStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType);
                    await using var fileWriter = File.OpenWrite(modelPath);
                    await modelStream.CopyToAsync(fileWriter);
                    downloadStopwatch.Stop();

                    var modelFileInfo = new FileInfo(modelPath);
                    logger.LogInformation(
                        "Whisper model downloaded. SubtitleTranscribeId: {SubtitleTranscribeId}, ModelSize: {ModelSize} bytes, Duration: {DurationMs}ms",
                        transcribe.Id,
                        modelFileInfo.Length,
                        downloadStopwatch.ElapsedMilliseconds);
                }
                else
                {
                    logger.LogDebug(
                        "Using existing Whisper model. SubtitleTranscribeId: {SubtitleTranscribeId}, ModelType: {ModelType}, Path: {ModelPath}",
                        transcribe.Id,
                        userSettings.ModelType,
                        modelPath);
                }

                // Transcribe audio using Whisper.net
                logger.LogInformation(
                    "Starting transcription. SubtitleTranscribeId: {SubtitleTranscribeId}, VideoId: {VideoId}, ModelType: {ModelType}, Language: {Language}, UseGpu: {UseGpu}",
                    transcribe.Id,
                    transcribe.VideoId,
                    userSettings.ModelType,
                    transcribe.Language,
                    userSettings.UseGpu);

                using var whisperFactory = WhisperFactory.FromPath(modelPath);

                // Note: GPU support is automatically enabled if Whisper.net.Runtime.Cuda is installed
                // No explicit GPU configuration needed - the library will use GPU if available
                if (userSettings.UseGpu)
                {
                    logger.LogInformation(
                        "GPU requested for transcription. SubtitleTranscribeId: {SubtitleTranscribeId} - will use GPU if CUDA runtime is available",
                        transcribe.Id);
                }

                var transcriptionStopwatch = System.Diagnostics.Stopwatch.StartNew();

                await using var processor = whisperFactory.CreateBuilder()
                    .WithLanguage((transcribe.Language == "auto" ? null : transcribe.Language) ?? "auto")
                    .Build();

                var segments = new List<(TimeSpan Start, TimeSpan End, string Text)>();

                await using var audioStream = File.OpenRead(audioPath);
                var segmentCount = 0;
                await foreach (var result in processor.ProcessAsync(audioStream))
                {
                    var start = result.Start;
                    var end = result.End;
                    segments.Add((start, end, result.Text));
                    segmentCount++;

                    if (segmentCount % 100 == 0)
                    {
                        logger.LogDebug(
                            "Transcription progress. SubtitleTranscribeId: {SubtitleTranscribeId}, SegmentsProcessed: {SegmentCount}",
                            transcribe.Id,
                            segmentCount);
                    }
                }

                transcriptionStopwatch.Stop();
                logger.LogInformation(
                    "Transcription processing completed. SubtitleTranscribeId: {SubtitleTranscribeId}, SegmentsCount: {SegmentCount}, Duration: {DurationMs}ms",
                    transcribe.Id,
                    segmentCount,
                    transcriptionStopwatch.ElapsedMilliseconds);

                // Convert to VTT format
                var vttContent = ConvertToVtt(segments);
                var vttFileName = $"{Guid.NewGuid()}.vtt";
                var vttPath = Path.Combine(_tempPath, vttFileName);

                await File.WriteAllTextAsync(vttPath, vttContent, Encoding.UTF8);

                // Save subtitle using VideoSubtitleService
                await using var vttStream = File.OpenRead(vttPath);
                var fileInfo = new FileInfo(vttPath);
                var languageDisplayName = GetLanguageDisplayName(transcribe.Language);

                var subtitle = await videoSubtitleService.UploadSubtitleAsync(
                    transcribe.VideoId,
                    languageDisplayName,
                    vttStream,
                    vttFileName,
                    fileInfo.Length,
                    transcribe.CreatedBy);

                // Update status to Completed
                transcribe.Status = VideoEnum.SubtitleTranscribeStatus.Completed;
                await subtitleTranscribeRepository.UpdateAsync(transcribe);

                logger.LogInformation(
                    "Transcription completed successfully. SubtitleTranscribeId: {SubtitleTranscribeId}, VideoId: {VideoId}, SegmentsCount: {SegmentCount}",
                    transcribe.Id,
                    transcribe.VideoId,
                    segments.Count);
            }
            finally
            {
                // Clean up temporary audio file
                if (File.Exists(audioPath))
                {
                    try
                    {
                        File.Delete(audioPath);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "Failed to delete temporary audio file. SubtitleTranscribeId: {SubtitleTranscribeId}, Path: {AudioPath}",
                            transcribe.Id,
                            audioPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error processing transcription. SubtitleTranscribeId: {SubtitleTranscribeId}, VideoId: {VideoId}",
                transcribe.Id,
                transcribe.VideoId);
            transcribe.Status = VideoEnum.SubtitleTranscribeStatus.Failed;
            transcribe.ErrorMessage = ex.Message;
            await subtitleTranscribeRepository.UpdateAsync(transcribe);
            throw;
        }
    }

    public async Task UpdateTranscribeStatusAsync(Guid id, VideoEnum.SubtitleTranscribeStatus status,
        string? errorMessage = null)
    {
        logger.LogDebug(
            "Updating transcribe status. SubtitleTranscribeId: {SubtitleTranscribeId}, Status: {Status}",
            id,
            status);

        var transcribe = await subtitleTranscribeRepository.GetByIdAsync(id);
        if (transcribe == null)
        {
            logger.LogWarning("SubtitleTranscribe not found for status update. SubtitleTranscribeId: {SubtitleTranscribeId}", id);
            throw new ArgumentException("SubtitleTranscribe không tồn tại", nameof(id));
        }

        transcribe.Status = status;
        transcribe.ErrorMessage = errorMessage;
        await subtitleTranscribeRepository.UpdateAsync(transcribe);

        logger.LogDebug(
            "Transcribe status updated. SubtitleTranscribeId: {SubtitleTranscribeId}, Status: {Status}",
            id,
            status);
    }

    private static string ConvertToVtt(List<(TimeSpan Start, TimeSpan End, string Text)> segments)
    {
        var sb = new StringBuilder();
        sb.AppendLine("WEBVTT");
        sb.AppendLine();

        foreach (var (start, end, text) in segments)
        {
            var startTime = FormatVttTime(start);
            var endTime = FormatVttTime(end);
            sb.AppendLine($"{startTime} --> {endTime}");
            sb.AppendLine(text.Trim());
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string FormatVttTime(TimeSpan time)
    {
        var hours = (int)time.TotalHours;
        var minutes = time.Minutes;
        var seconds = time.Seconds;
        var milliseconds = time.Milliseconds;

        return $"{hours:D2}:{minutes:D2}:{seconds:D2}.{milliseconds:D3}";
    }

    private static string GetLanguageDisplayName(string languageCode)
    {
        var supportedLanguages = LanguageHelper.GetSupportedLanguages();
        return supportedLanguages.FirstOrDefault(language => language.Code == languageCode)?.NativeName ?? languageCode;
    }
}
