using AsyncAwaitBestPractices;
using EzioHost.Core.Services.Interface;
using EzioHost.Shared.Events;
using EzioHost.Shared.HubActions;
using EzioHost.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Quartz;

namespace EzioHost.WebAPI.Jobs;

[DisallowConcurrentExecution]
public class VideoUpscaleJob(
    IUpscaleService upscaleService,
    IHubContext<VideoHub,
        IVideoHubAction> videoHub) : IJob, IDisposable
{
    private Guid _userId;

    public void Dispose()
    {
        upscaleService.OnVideoUpscaleStreamAdded -= UpscaleService_OnVideoUpscaleStreamAdded;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var videoUpscale = await upscaleService.GetVideoNeedUpscale();

        if (videoUpscale == null) return;

        _userId = videoUpscale.CreatedBy;

        upscaleService.OnVideoUpscaleStreamAdded += UpscaleService_OnVideoUpscaleStreamAdded;


        await upscaleService.UpscaleVideo(videoUpscale);
    }

    private void UpscaleService_OnVideoUpscaleStreamAdded(object? sender,
        VideoStreamAddedEventArgs videoStreamAddedEventArgs)
    {
        if (_userId == Guid.Empty) return;

        videoHub.Clients.User(_userId.ToString()).ReceiveNewVideoStream(videoStreamAddedEventArgs).SafeFireAndForget();
    }
}