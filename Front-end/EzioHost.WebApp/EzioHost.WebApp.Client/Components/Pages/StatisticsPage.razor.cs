using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EzioHost.WebApp.Client.Components.Pages;

[Authorize]
public partial class StatisticsPage : IAsyncDisposable
{
    private IJSObjectReference? _jsModule;
    [Inject] public IVideoApi VideoApi { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;

    private VideoDetailedStatisticsDto? Statistics { get; set; }
    private VideoStatisticsDto? QuickStats { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; }

    private int TotalVideos => QuickStats?.TotalVideos ?? 0;
    private int ReadyVideos => QuickStats?.ReadyVideos ?? 0;
    private string StorageUsedDisplay => QuickStats?.TotalStorageUsedDisplay ?? "-";

    public async ValueTask DisposeAsync()
    {
        // Destroy all chart instances
        if (_jsModule != null)
            try
            {
                // Destroy charts through JS module
                await _jsModule.InvokeVoidAsync("destroyChart", "videoTimelineChart");
                await _jsModule.InvokeVoidAsync("destroyChart", "storageTimelineChart");
                await _jsModule.InvokeVoidAsync("destroyChart", "resolutionPieChart");
                await _jsModule.InvokeVoidAsync("destroyChart", "statusPieChart");

                await _jsModule.DisposeAsync();
            }
            catch
            {
                // Ignore disposal errors
            }
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Load quick stats
            QuickStats = await VideoApi.GetStatistics();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not load quick statistics: {ex.Message}");
        }

        try
        {
            Statistics = await VideoApi.GetDetailedStatistics();
            HasError = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not load detailed statistics: {ex.Message}");
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            try
            {
                _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./Components/Pages/StatisticsPage.razor.js");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load JS module: {ex.Message}");
            }

        if (!IsLoading && !HasError && Statistics != null && _jsModule != null) await RenderCharts();
    }

    private async Task RenderCharts()
    {
        if (_jsModule == null || Statistics == null) return;

        try
        {
            // Render Video Timeline Chart
            if (Statistics.VideoTimeline.Any())
            {
                var videoLabels = Statistics.VideoTimeline.Select(t => t.Date).ToArray();
                var videoData = Statistics.VideoTimeline.Select(t => t.Count).ToArray();

                await _jsModule.InvokeVoidAsync(
                    "initVideoTimelineChart", "videoTimelineChart", videoLabels, videoData);
            }

            // Render Storage Timeline Chart
            if (Statistics.StorageTimeline.Any())
            {
                var storageLabels = Statistics.StorageTimeline.Select(t => t.Date).ToArray();
                var storageData = Statistics.StorageTimeline.Select(t => t.StorageBytes).ToArray();

                await _jsModule.InvokeVoidAsync(
                    "initStorageTimelineChart", "storageTimelineChart", storageLabels, storageData);
            }

            // Render Resolution Pie Chart
            if (Statistics.ResolutionDistribution.Any())
            {
                var resolutionLabels = Statistics.ResolutionDistribution.Select(d => d.Label).ToArray();
                var resolutionData = Statistics.ResolutionDistribution.Select(d => d.Count).ToArray();

                await _jsModule.InvokeVoidAsync(
                    "initResolutionPieChart", "resolutionPieChart", resolutionLabels, resolutionData);
            }

            // Render Status Pie Chart
            if (Statistics.StatusDistribution.Any())
            {
                var statusLabels = Statistics.StatusDistribution.Select(d => d.Label).ToArray();
                var statusData = Statistics.StatusDistribution.Select(d => d.Count).ToArray();

                await _jsModule.InvokeVoidAsync(
                    "initStatusPieChart", "statusPieChart", statusLabels, statusData);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rendering charts: {ex.Message}");
        }
    }
}