using EzioHost.Shared.Enums;
using EzioHost.Shared.Extensions;
using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Extensions;
using EzioHost.WebApp.Client.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EzioHost.WebApp.Client.Components.Pages;

[Authorize]
public partial class EncodingSettingsPage : IAsyncDisposable
{
    [Inject] public IEncodingQualitySettingApi EncodingApi { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    private List<EncodingQualitySettingDto> Settings { get; set; } = [];
    private bool IsLoading { get; set; } = true;
    private bool IsSaving { get; set; } = false;
    private IJSObjectReference? _jsModule;

    // All available resolutions
    private List<ResolutionInfo> AllResolutions { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Initialize all resolutions
            AllResolutions = Enum.GetValues<VideoEnum.VideoResolution>()
                .Where(r => r != VideoEnum.VideoResolution.Upscaled)
                .Select(r => new ResolutionInfo { Resolution = r })
                .OrderBy(r => (int)r.Resolution)
                .ToList();

            await LoadSettings();
        }
        catch (Exception ex)
        {
            await JsRuntime.ShowErrorToast($"Lỗi khi tải cài đặt: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Pages/EncodingSettingsPage.razor.js");
        }
    }

    private async Task LoadSettings()
    {
        try
        {
            Settings = await EncodingApi.GetSettings() ?? [];
            
            // Ensure all resolutions have a setting entry
            foreach (var resolution in AllResolutions)
            {
                if (!Settings.Any(s => s.Resolution == resolution.Resolution))
                {
                    Settings.Add(new EncodingQualitySettingDto
                    {
                        Id = Guid.NewGuid(),
                        Resolution = resolution.Resolution,
                        BitrateKbps = GetDefaultBitrate(resolution.Resolution),
                        IsEnabled = false
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await JsRuntime.ShowErrorToast($"Không thể tải cài đặt: {ex.Message}");
        }
    }

    private void ToggleResolution(VideoEnum.VideoResolution resolution, bool isEnabled)
    {
        var setting = Settings.FirstOrDefault(s => s.Resolution == resolution);
        if (setting != null)
        {
            setting.IsEnabled = isEnabled;
        }
        else
        {
            Settings.Add(new EncodingQualitySettingDto
            {
                Id = Guid.NewGuid(),
                Resolution = resolution,
                BitrateKbps = GetDefaultBitrate(resolution),
                IsEnabled = isEnabled
            });
        }
        StateHasChanged();
    }

    private void UpdateBitrate(VideoEnum.VideoResolution resolution, int bitrate)
    {
        var setting = Settings.FirstOrDefault(s => s.Resolution == resolution);
        if (setting != null)
        {
            setting.BitrateKbps = Math.Max(100, Math.Min(50000, bitrate));
        }
        else
        {
            Settings.Add(new EncodingQualitySettingDto
            {
                Id = Guid.NewGuid(),
                Resolution = resolution,
                BitrateKbps = Math.Max(100, Math.Min(50000, bitrate)),
                IsEnabled = true
            });
        }
        StateHasChanged();
    }

    private async Task SaveSettings()
    {
        if (IsSaving) return;

        // Validate: At least one resolution must be enabled
        var enabledCount = Settings.Count(s => s.IsEnabled);
        if (enabledCount == 0)
        {
            await JsRuntime.ShowErrorToast("Vui lòng chọn ít nhất một độ phân giải để encode!");
            return;
        }

        IsSaving = true;
        StateHasChanged();

        try
        {
            var request = new EncodingQualitySettingUpdateRequest
            {
                Settings = Settings.Select(s => new EncodingQualitySettingUpdateItem
                {
                    Id = s.Id,
                    Resolution = s.Resolution,
                    BitrateKbps = s.BitrateKbps,
                    IsEnabled = s.IsEnabled
                }).ToList()
            };

            Settings = await EncodingApi.UpdateSettings(request) ?? [];
            await JsRuntime.ShowSuccessToast("Đã lưu cài đặt thành công!");

            if (_jsModule != null)
            {
                await _jsModule.InvokeVoidAsync("onSettingsSaved");
            }
        }
        catch (Exception ex)
        {
            await JsRuntime.ShowErrorToast($"Lỗi khi lưu cài đặt: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
            StateHasChanged();
        }
    }

    private async Task ResetToDefaults()
    {
        if (!await JsRuntime.Confirm("Bạn có chắc muốn đặt lại tất cả cài đặt về mặc định?"))
        {
            return;
        }

        Settings.Clear();
        foreach (var resolution in AllResolutions)
        {
            var defaultEnabled = resolution.Resolution is 
                VideoEnum.VideoResolution._360p or 
                VideoEnum.VideoResolution._480p or 
                VideoEnum.VideoResolution._720p;

            Settings.Add(new EncodingQualitySettingDto
            {
                Id = Guid.NewGuid(),
                Resolution = resolution.Resolution,
                BitrateKbps = GetDefaultBitrate(resolution.Resolution),
                IsEnabled = defaultEnabled
            });
        }

        StateHasChanged();
        await SaveSettings();
    }

    private int GetDefaultBitrate(VideoEnum.VideoResolution resolution)
    {
        return resolution switch
        {
            VideoEnum.VideoResolution._360p => 800,
            VideoEnum.VideoResolution._480p => 1400,
            VideoEnum.VideoResolution._720p => 2800,
            VideoEnum.VideoResolution._1080p => 5000,
            VideoEnum.VideoResolution._1440p => 8000,
            VideoEnum.VideoResolution._1920p => 8000,
            VideoEnum.VideoResolution._2160p => 15000,
            _ => 1000
        };
    }

    private string GetResolutionName(VideoEnum.VideoResolution resolution)
    {
        return resolution.GetDescription();
    }

    private string GetResolutionDimensions(VideoEnum.VideoResolution resolution)
    {
        return resolution switch
        {
            VideoEnum.VideoResolution._360p => "640 x 360",
            VideoEnum.VideoResolution._480p => "854 x 480",
            VideoEnum.VideoResolution._720p => "1280 x 720",
            VideoEnum.VideoResolution._1080p => "1920 x 1080",
            VideoEnum.VideoResolution._1440p => "2560 x 1440",
            VideoEnum.VideoResolution._1920p => "2560 x 1920",
            VideoEnum.VideoResolution._2160p => "3840 x 2160",
            _ => "N/A"
        };
    }

    private int EnabledCount => Settings?.Count(s => s.IsEnabled) ?? 0;

    private int AverageBitrate
    {
        get
        {
            var enabledSettings = Settings.Where(s => s.IsEnabled).ToList();
            if (!enabledSettings.Any()) return 0;
            return (int)enabledSettings.Average(s => s.BitrateKbps);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule != null)
        {
            try
            {
                await _jsModule.DisposeAsync();
            }
            catch
            {
                // Ignore
            }
        }
    }

    private class ResolutionInfo
    {
        public VideoEnum.VideoResolution Resolution { get; set; }
    }
}
