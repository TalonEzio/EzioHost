namespace EzioHost.Shared.Models;

public class VideoDetailedStatisticsDto
{
    public List<VideoTimeSeriesDto> VideoTimeline { get; set; } = [];
    public List<VideoTimeSeriesDto> StorageTimeline { get; set; } = [];
    public List<VideoDistributionDto> ResolutionDistribution { get; set; } = [];
    public List<VideoDistributionDto> StatusDistribution { get; set; } = [];
}

public class VideoTimeSeriesDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
    public long StorageBytes { get; set; }
}

public class VideoDistributionDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}
