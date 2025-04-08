using EzioHost.Core.Services.Interface;
using Quartz;

namespace EzioHost.WebAPI.Jobs
{
    [DisallowConcurrentExecution]
    public class VideoUpscaleJob(IUpscaleService upscaleService) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var videoUpscale = await upscaleService.GetVideoNeedUpscale();

            if (videoUpscale == null) return;

            await upscaleService.UpscaleVideo(videoUpscale);
        }
    }
}
