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
        try
        {
            logger.LogInformation("[VideoBackupJob] Starting video backup scan...");

            var videosToBackup = await videoService.GetVideoBackup();

            if (videosToBackup == null)
            {
                logger.LogInformation("[VideoBackupJob] No videos to backup.");
                return;
            }

            logger.LogInformation("[VideoBackupJob] Starting backup for video {VideoId}", videosToBackup.Id);
            var backupStatus = await videoService.BackupVideo(videosToBackup);

            if (backupStatus == VideoEnum.VideoBackupStatus.BackedUp)
            {
                logger.LogInformation("[VideoBackupJob] Successfully backed up video {VideoId}", videosToBackup.Id);
            }
            else
            {
                logger.LogWarning("[VideoBackupJob] Failed to backup video {VideoId}", videosToBackup.Id);
            }

            logger.LogInformation("[VideoBackupJob] Completed video backup scan.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[VideoBackupJob] Error during video backup scan.");
        }
    }
}