using AsyncAwaitBestPractices;
using AutoMapper;
using EzioHost.Core.Services.Interface;
using EzioHost.Shared.HubActions;
using EzioHost.Shared.Models;
using EzioHost.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Quartz;

namespace EzioHost.WebAPI.Jobs;

[DisallowConcurrentExecution]
public class SubtitleTranscribeJob(
    ISubtitleTranscribeService subtitleTranscribeService,
    IVideoSubtitleService videoSubtitleService,
    IHubContext<VideoHub, IVideoHubAction> videoHub,
    IMapper mapper,
    ILogger<SubtitleTranscribeJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var transcribe = await subtitleTranscribeService.GetNextTranscribeJobAsync();

            if (transcribe == null) return;

            logger.LogInformation($"[SubtitleTranscribeJob] Processing transcription for video {transcribe.VideoId}");

            var userId = transcribe.CreatedBy;

            await subtitleTranscribeService.ProcessTranscriptionAsync(transcribe);

            // Get the created subtitle and send SignalR notification
            var subtitles = await videoSubtitleService.GetSubtitlesByVideoIdAsync(transcribe.VideoId);
            var latestSubtitle = subtitles.OrderByDescending(s => s.CreatedAt).FirstOrDefault();

            if (latestSubtitle != null)
            {
                var subtitleDto = mapper.Map<VideoSubtitleDto>(latestSubtitle);
                videoHub.Clients.User(userId.ToString())
                    .ReceiveSubtitleTranscribed(subtitleDto)
                    .SafeFireAndForget();

                logger.LogInformation(
                    $"[SubtitleTranscribeJob] Transcription completed and notified user {userId} for video {transcribe.VideoId}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SubtitleTranscribeJob] Error processing transcription");
        }
    }
}
