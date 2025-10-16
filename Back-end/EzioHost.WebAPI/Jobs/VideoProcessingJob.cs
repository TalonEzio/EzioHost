using AsyncAwaitBestPractices;
using EzioHost.Core.Services.Interface;
using EzioHost.Shared.Events;
using EzioHost.Shared.Hubs;
using EzioHost.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Quartz;
using System.Diagnostics;

namespace EzioHost.WebAPI.Jobs
{
    [DisallowConcurrentExecution]
    public class VideoProcessingJob(IVideoService videoService, ILogger<VideoProcessingJob> logger, IHubContext<VideoHub, IVideoHubAction> videoHub) : IJob, IDisposable
    {
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
                    await videoService.EncodeVideo(videoToEncode);
                }
                else
                {
                    logger.LogInformation("[VideoProcessingJob] No videos to encode.");
                }
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

        private void VideoServiceOnOnVideoProcessDone(VideoProcessDoneEvent obj)
        {
            videoHub.Clients.All.ReceiveVideoProcessingDone(obj).SafeFireAndForget();
        }

        private void VideoServiceOnOnVideoStreamAdded(VideoStreamAddedEvent obj)
        {
            videoHub.Clients.All.ReceiveNewVideoStream(obj).SafeFireAndForget();
        }

        public void Dispose()
        {
            videoService.OnVideoStreamAdded -= VideoServiceOnOnVideoStreamAdded;
        }
    }
}