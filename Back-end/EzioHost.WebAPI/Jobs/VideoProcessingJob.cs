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
    IHubContext<VideoHub, IVideoHubAction> videoHub) : IJob, IDisposable
{
    private Guid _userId;
    public void Dispose()
    {
        videoService.OnVideoStreamAdded -= VideoServiceOnOnVideoStreamAdded;
        videoService.OnVideoProcessDone -= VideoServiceOnOnVideoProcessDone;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        videoService.OnVideoStreamAdded += VideoServiceOnOnVideoStreamAdded;
        videoService.OnVideoProcessDone += VideoServiceOnOnVideoProcessDone;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var videoToEncode = await videoService.GetVideoToEncode();

            if (videoToEncode != null)
            {
                logger.LogInformation($"[VideoProcessingJob] Encoding video ID: {videoToEncode.Id}");
                _userId = videoToEncode.CreatedBy;
                await videoService.EncodeVideo(videoToEncode);
            }
            //logger.LogInformation("[VideoProcessingJob] No videos to encode.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[VideoProcessingJob] Error during video processing.");
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation($"[VideoProcessingJob] Finished in {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    private void VideoServiceOnOnVideoProcessDone(VideoProcessDoneEvent args)
    {
        if (_userId == Guid.Empty) return;

        videoHub.Clients.User(_userId.ToString()).ReceiveVideoProcessingDone(args).SafeFireAndForget();
    }

    private void VideoServiceOnOnVideoStreamAdded(VideoStreamAddedEventArgs obj)
    {
        if (_userId == Guid.Empty) return;

        videoHub.Clients.User(_userId.ToString()).ReceiveNewVideoStream(obj).SafeFireAndForget();
    }
}