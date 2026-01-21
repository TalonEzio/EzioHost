using EzioHost.Core.Services.Interface;
using EzioHost.Shared.Enums;
using Quartz;

namespace EzioHost.WebAPI.Jobs;

[DisallowConcurrentExecution]
public class VideoBackupJob(
    IVideoService videoService,
    ILogger<VideoBackupJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var jobName = context.JobDetail.Key.Name;
        var triggerName = context.Trigger.Key.Name;

        logger.LogInformation(
            "VideoBackupJob started. JobName: {JobName}, TriggerName: {TriggerName}, FireTime: {FireTime}",
            jobName,
            triggerName,
            context.FireTimeUtc);

        try
        {
            var videosToBackup = await videoService.GetVideoBackup();

            if (videosToBackup == null)
            {
                logger.LogDebug("No videos to backup");
                return;
            }

            logger.LogInformation(
                "Starting backup for video. VideoId: {VideoId}, Title: {Title}, CreatedBy: {CreatedBy}",
                videosToBackup.Id,
                videosToBackup.Title,
                videosToBackup.CreatedBy);

            var backupStatus = await videoService.BackupVideo(videosToBackup);

            if (backupStatus == VideoEnum.VideoBackupStatus.BackedUp)
            {
                logger.LogInformation(
                    "Successfully backed up video. VideoId: {VideoId}, Duration: {DurationMs}ms",
                    videosToBackup.Id,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogWarning(
                    "Video backup failed or incomplete. VideoId: {VideoId}, Status: {Status}, Duration: {DurationMs}ms",
                    videosToBackup.Id,
                    backupStatus,
                    stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Error during video backup. Duration: {DurationMs}ms",
                stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "VideoBackupJob finished. JobName: {JobName}, Duration: {DurationMs}ms",
                jobName,
                stopwatch.ElapsedMilliseconds);
        }
    }
}