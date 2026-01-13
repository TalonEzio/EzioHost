namespace EzioHost.Shared.Models;

public class VideoStatisticsDto
{
    public int TotalVideos { get; set; }
    public int ReadyVideos { get; set; }
    public long TotalStorageUsedBytes { get; set; }
    
    public string TotalStorageUsedDisplay
    {
        get
        {
            if (TotalStorageUsedBytes >= 1024L * 1024 * 1024 * 1024) // >= 1TB
                return $"{TotalStorageUsedBytes / (1024.0 * 1024 * 1024 * 1024):F2} TB";
            if (TotalStorageUsedBytes >= 1024L * 1024 * 1024) // >= 1GB
                return $"{TotalStorageUsedBytes / (1024.0 * 1024 * 1024):F2} GB";
            if (TotalStorageUsedBytes >= 1024L * 1024) // >= 1MB
                return $"{TotalStorageUsedBytes / (1024.0 * 1024):F2} MB";
            return $"{TotalStorageUsedBytes / 1024.0:F2} KB";
        }
    }
}
