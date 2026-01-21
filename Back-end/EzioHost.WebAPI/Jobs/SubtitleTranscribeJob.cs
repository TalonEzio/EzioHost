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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var jobName = context.JobDetail.Key.Name;
        var triggerName = context.Trigger.Key.Name;

        logger.LogInformation(
            "SubtitleTranscribeJob started. JobName: {JobName}, TriggerName: {TriggerName}, FireTime: {FireTime}",
            jobName,
            triggerName,
            context.FireTimeUtc);

        try
        {
            var transcribe = await subtitleTranscribeService.GetNextTranscribeJobAsync();

            if (transcribe == null)
            {
                logger.LogDebug("No transcription jobs to process");
                return;
            }

            logger.LogInformation(
                "Processing transcription. SubtitleTranscribeId: {SubtitleTranscribeId}, VideoId: {VideoId}, Language: {Language}, UserId: {UserId}",
                transcribe.Id,
                transcribe.VideoId,
                transcribe.Language,
                transcribe.CreatedBy);

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
                    "Transcription completed and notification sent. SubtitleTranscribeId: {SubtitleTranscribeId}, VideoId: {VideoId}, SubtitleId: {SubtitleId}, UserId: {UserId}, Duration: {DurationMs}ms",
                    transcribe.Id,
                    transcribe.VideoId,
                    latestSubtitle.Id,
                    userId,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogWarning(
                    "Transcription completed but no subtitle found. SubtitleTranscribeId: {SubtitleTranscribeId}, VideoId: {VideoId}",
                    transcribe.Id,
                    transcribe.VideoId);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Error processing transcription. Duration: {DurationMs}ms",
                stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "SubtitleTranscribeJob finished. JobName: {JobName}, Duration: {DurationMs}ms",
                jobName,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
