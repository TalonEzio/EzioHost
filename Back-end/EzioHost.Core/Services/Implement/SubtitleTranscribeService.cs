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
        // Check user settings - IsEnabled
        var userSettings = await subtitleTranscribeSettingService.GetUserSettingsAsync(userId);
        if (!userSettings.IsEnabled)
            throw new InvalidOperationException("Tính năng Audio Transcribing đã bị tắt. Vui lòng bật trong Settings.");

        // Check if video exists and is ready
        var video = await videoRepository.GetVideoById(videoId);
        if (video == null) throw new ArgumentException("Video không tồn tại", nameof(videoId));

        if (video.Status != VideoEnum.VideoStatus.Ready)
            throw new InvalidOperationException("Video phải ở trạng thái Ready mới có thể transcribe");

        // Check if video already has subtitles
        var existingSubtitles = await videoSubtitleService.GetSubtitlesByVideoIdAsync(videoId);
        if (existingSubtitles.Any())
            throw new InvalidOperationException(
                "Video đã có subtitle. Vui lòng xóa subtitle cũ trước khi transcribe lại.");

        // Check if there's already a pending/processing transcribe request
        var existingRequest = await subtitleTranscribeRepository.ExistsByVideoIdAsync(videoId);
        if (existingRequest)
            throw new InvalidOperationException("Video đã có request transcribe đang chờ xử lý");

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

        return await subtitleTranscribeRepository.AddAsync(transcribe);
    }

    public Task<SubtitleTranscribe?> GetNextTranscribeJobAsync()
    {
        return subtitleTranscribeRepository.GetNextJobAsync();
    }

    public async Task ProcessTranscriptionAsync(SubtitleTranscribe transcribe)
    {
        try
        {
            // Update status to Processing
            transcribe.Status = VideoEnum.SubtitleTranscribeStatus.Processing;
            await subtitleTranscribeRepository.UpdateAsync(transcribe);

            var video = transcribe.Video;

            var videoPath = Path.Combine(_webRootPath, video.RawLocation);
            if (!File.Exists(videoPath))
                throw new FileNotFoundException($"Video file không tồn tại: {videoPath}");

            // Extract audio from video using FFmpeg
            var audioFileName = $"{Guid.NewGuid()}.wav";
            var audioPath = Path.Combine(_tempPath, audioFileName);

            logger.LogInformation($"[SubtitleTranscribe] Extracting audio from video {transcribe.VideoId}");

            var audioExtractionArguments = FFMpegArguments
                .FromFileInput(videoPath)
                .OutputToFile(audioPath, true, options => options
                    .WithCustomArgument("-f wav")
                    .WithCustomArgument("-acodec pcm_s16le")
                    .WithCustomArgument("-ar 16000") // 16kHz sample rate
                    .WithCustomArgument("-ac 1") // Mono channel
                );

            await audioExtractionArguments.ProcessAsynchronously();

            if (!File.Exists(audioPath))
                throw new InvalidOperationException("Không thể extract audio từ video");

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
                    logger.LogInformation($"[SubtitleTranscribe] Downloading Whisper model {userSettings.ModelType} to {modelPath}");
                    await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType);
                    await using var fileWriter = File.OpenWrite(modelPath);
                    await modelStream.CopyToAsync(fileWriter);
                }

                // Transcribe audio using Whisper.net
                logger.LogInformation($"[SubtitleTranscribe] Starting transcription for video {transcribe.VideoId} with model {userSettings.ModelType}");

                using var whisperFactory = WhisperFactory.FromPath(modelPath);

                // Note: GPU support is automatically enabled if Whisper.net.Runtime.Cuda is installed
                // No explicit GPU configuration needed - the library will use GPU if available
                if (userSettings.UseGpu)
                {
                    logger.LogInformation($"[SubtitleTranscribe] GPU requested - will use GPU if CUDA runtime is available");
                }

                await using var processor = whisperFactory.CreateBuilder()
                    .WithLanguage((transcribe.Language == "auto" ? null : transcribe.Language) ?? "auto")
                    .Build();

                var segments = new List<(TimeSpan Start, TimeSpan End, string Text)>();

                await using var audioStream = File.OpenRead(audioPath);
                await foreach (var result in processor.ProcessAsync(audioStream))
                {
                    var start = result.Start;
                    var end = result.End;
                    segments.Add((start, end, result.Text));
                }

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
                    $"[SubtitleTranscribe] Transcription completed successfully for video {transcribe.VideoId}");
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
                        logger.LogWarning(ex, $"[SubtitleTranscribe] Failed to delete temporary audio file: {audioPath}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"[SubtitleTranscribe] Error processing transcription for video {transcribe.VideoId}");
            transcribe.Status = VideoEnum.SubtitleTranscribeStatus.Failed;
            transcribe.ErrorMessage = ex.Message;
            await subtitleTranscribeRepository.UpdateAsync(transcribe);
            throw;
        }
    }

    public async Task UpdateTranscribeStatusAsync(Guid id, VideoEnum.SubtitleTranscribeStatus status,
        string? errorMessage = null)
    {
        var transcribe = await subtitleTranscribeRepository.GetByIdAsync(id);
        if (transcribe == null) throw new ArgumentException("SubtitleTranscribe không tồn tại", nameof(id));

        transcribe.Status = status;
        transcribe.ErrorMessage = errorMessage;
        await subtitleTranscribeRepository.UpdateAsync(transcribe);
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
