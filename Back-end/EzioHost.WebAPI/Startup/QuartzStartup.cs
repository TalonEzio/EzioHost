using EzioHost.WebAPI.Jobs;
using Quartz;

namespace EzioHost.WebAPI.Startup;

public static class QuartzStartup
{
    public static WebApplicationBuilder ConfigureQuartz(this WebApplicationBuilder builder)
    {
        builder.Services.AddQuartz(quartz =>
        {
            var videoProcessingJobKey = new JobKey(nameof(VideoProcessingJob));
            quartz.AddJob<VideoProcessingJob>(opts => opts.WithIdentity(videoProcessingJobKey).StoreDurably());

            quartz.AddTrigger(cfg => cfg
                .WithIdentity(nameof(VideoProcessingJob))
                .ForJob(videoProcessingJobKey)
                .StartNow()
                .WithSimpleSchedule(schedule => schedule
                    .WithIntervalInSeconds(10)
                    .RepeatForever()
                )
            );

            var videoUpscaleJobKey = new JobKey(nameof(VideoUpscaleJob));
            quartz.AddJob<VideoUpscaleJob>(opts => opts.WithIdentity(videoUpscaleJobKey).StoreDurably());

            quartz.AddTrigger(cfg => cfg
                .WithIdentity(nameof(VideoUpscaleJob))
                .ForJob(videoUpscaleJobKey)
                .StartNow()
                .WithSimpleSchedule(schedule => schedule
                    .WithIntervalInSeconds(10)
                    .RepeatForever()
                )
            );

            var videoBackupJobKey = new JobKey(nameof(VideoBackupJob));
            quartz.AddJob<VideoBackupJob>(opts => opts.WithIdentity(videoBackupJobKey).StoreDurably());

            quartz.AddTrigger(cfg => cfg
                .WithIdentity(nameof(VideoBackupJob))
                .ForJob(videoBackupJobKey)
                .StartNow()
                .WithSimpleSchedule(schedule => schedule
                    .WithIntervalInSeconds(10)
                    .RepeatForever()
                )
            );
        });
        builder.Services.AddQuartzHostedService(cfg =>
        {
            cfg.AwaitApplicationStarted = true;
            cfg.WaitForJobsToComplete = true;
        });

        return builder;
    }
}