using System.Diagnostics;
using AsyncAwaitBestPractices;
using EzioHost.Core.Services.Interface;
using EzioHost.Shared.Events;
using EzioHost.Shared.HubActions;
using EzioHost.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Quartz;

namespace EzioHost.WebAPI.Jobs;

[DisallowConcurrentExecution]
public class VideoProcessingJob(
    IVideoService videoService,
    ILogger<VideoProcessingJob> logger,
    IHubContext<VideoHub, IVideoHubAction> videoHub) : IJob
{
    private Guid _userId;

    public async Task Execute(IJobExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var jobName = context.JobDetail.Key.Name;
        var triggerName = context.Trigger.Key.Name;

        logger.LogInformation(
            "VideoProcessingJob started. JobName: {JobName}, TriggerName: {TriggerName}, FireTime: {FireTime}",
            jobName,
            triggerName,
            context.FireTimeUtc);

        try
        {
            videoService.OnVideoStreamAdded += VideoServiceOnOnVideoStreamAdded;
            videoService.OnVideoProcessDone += VideoServiceOnOnVideoProcessDone;

            var videoToEncode = await videoService.GetVideoToEncode();

            if (videoToEncode != null)
            {
                logger.LogInformation(
                    "Encoding video. VideoId: {VideoId}, Title: {Title}, CreatedBy: {CreatedBy}",
                    videoToEncode.Id,
                    videoToEncode.Title,
                    videoToEncode.CreatedBy);

                _userId = videoToEncode.CreatedBy;
                await videoService.EncodeVideo(videoToEncode);

                logger.LogInformation(
                    "Video encoding completed successfully. VideoId: {VideoId}, Duration: {DurationMs}ms",
                    videoToEncode.Id,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogDebug("No videos to encode");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error during video processing. Duration: {DurationMs}ms",
                stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "VideoProcessingJob finished. JobName: {JobName}, Duration: {DurationMs}ms",
                jobName,
                stopwatch.ElapsedMilliseconds);

            videoService.OnVideoStreamAdded -= VideoServiceOnOnVideoStreamAdded;
            videoService.OnVideoProcessDone -= VideoServiceOnOnVideoProcessDone;
        }
    }

    private void VideoServiceOnOnVideoProcessDone(object? sender, VideoProcessDoneEvent args)
    {
        if (_userId == Guid.Empty) return;

        logger.LogDebug(
            "Sending video processing done notification via SignalR. VideoId: {VideoId}, UserId: {UserId}",
            args.Video.Id,
            _userId);

        videoHub.Clients.User(_userId.ToString()).ReceiveVideoProcessingDone(args).SafeFireAndForget();
    }

    private void VideoServiceOnOnVideoStreamAdded(object? sender, VideoStreamAddedEventArgs obj)
    {
        if (_userId == Guid.Empty) return;

        logger.LogDebug(
            "Sending video stream added notification via SignalR. VideoId: {VideoId}, VideoStreamId: {VideoStreamId}, UserId: {UserId}",
            obj.VideoId,
            obj.VideoStream.Id,
            _userId);

        videoHub.Clients.User(_userId.ToString()).ReceiveNewVideoStream(obj).SafeFireAndForget();
    }
}