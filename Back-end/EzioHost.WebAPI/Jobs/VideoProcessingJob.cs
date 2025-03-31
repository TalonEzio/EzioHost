using System.Diagnostics;
using EzioHost.Core.Services.Interface;
using Quartz;

namespace EzioHost.WebAPI.Jobs
{
    [DisallowConcurrentExecution]
    public class VideoProcessingJob(IVideoService videoService, ILogger<VideoProcessingJob> logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
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
    }
}