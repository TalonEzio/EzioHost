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
public partial class SubtitleTranscribeSettingsPage : IAsyncDisposable
{
    private IJSObjectReference? _jsModule;
    [Inject] public IVideoSubtitleApi VideoSubtitleApi { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    private SubtitleTranscribeSettingDto Settings { get; set; } = new();
    private bool IsLoading { get; set; } = true;
    private bool IsSaving { get; set; }
    private string? SuccessMessage { get; set; }
    private string? ErrorMessage { get; set; }

    private WhisperEnum.WhisperModelType SelectedModelType
    {
        get => Settings.ModelType;
        set => Settings.ModelType = value;
    }

    private int? GpuDeviceId
    {
        get => Settings.GpuDeviceId;
        set => Settings.GpuDeviceId = value;
    }

    private List<ModelInfo> AvailableModels { get; set; } = new()
    {
        new ModelInfo { Type = WhisperEnum.WhisperModelType.Tiny, Name = "Tiny", Size = "~75MB", Speed = "Nhanh nhất", Accuracy = "Thấp" },
        new ModelInfo { Type = WhisperEnum.WhisperModelType.Base, Name = "Base", Size = "~142MB", Speed = "Nhanh", Accuracy = "Cân bằng" },
        new ModelInfo { Type = WhisperEnum.WhisperModelType.Small, Name = "Small", Size = "~466MB", Speed = "Trung bình", Accuracy = "Tốt" },
        new ModelInfo { Type = WhisperEnum.WhisperModelType.Medium, Name = "Medium", Size = "~1.5GB", Speed = "Chậm", Accuracy = "Cao" },
        new ModelInfo { Type = WhisperEnum.WhisperModelType.Large, Name = "Large", Size = "~2.9GB", Speed = "Chậm nhất", Accuracy = "Cao nhất" }
    };

    private ModelInfo? SelectedModelInfo => AvailableModels.FirstOrDefault(m => m.Type == SelectedModelType);

    public async ValueTask DisposeAsync()
    {
        if (_jsModule != null)
            try
            {
                await _jsModule.DisposeAsync();
            }
            catch
            {
                // Ignore
            }
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadSettings();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi tải cài đặt: {ex.Message}";
            await JsRuntime.ShowErrorToast(ErrorMessage);
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
                _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import",
                    "./Components/Pages/SubtitleTranscribeSettingsPage.razor.js");
            }
            catch
            {
                // Ignore if JS module doesn't exist
            }
    }

    private async Task LoadSettings()
    {
        try
        {
            Settings = await VideoSubtitleApi.GetTranscribeSettings();
            ErrorMessage = null;
            SuccessMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải cài đặt: {ex.Message}";
            await JsRuntime.ShowErrorToast(ErrorMessage);
        }
    }

    private void OnModelTypeChanged()
    {
        StateHasChanged();
    }

    private void OnGpuDeviceIdChanged()
    {
        StateHasChanged();
    }

    private async Task SaveSettings()
    {
        if (IsSaving) return;

        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;
        StateHasChanged();

        try
        {
            var updateDto = new SubtitleTranscribeSettingUpdateDto
            {
                IsEnabled = Settings.IsEnabled,
                ModelType = Settings.ModelType,
                UseGpu = Settings.UseGpu,
                GpuDeviceId = Settings.GpuDeviceId
            };

            Settings = await VideoSubtitleApi.UpdateTranscribeSettings(updateDto);
            SuccessMessage = "Đã lưu cài đặt thành công!";
            await JsRuntime.ShowSuccessToast(SuccessMessage);

            if (_jsModule != null) await _jsModule.InvokeVoidAsync("onSettingsSaved");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi khi lưu cài đặt: {ex.Message}";
            await JsRuntime.ShowErrorToast(ErrorMessage);
        }
        finally
        {
            IsSaving = false;
            StateHasChanged();
        }
    }

    private async Task ResetToDefaults()
    {
        if (!await JsRuntime.Confirm("Bạn có chắc muốn đặt lại tất cả cài đặt về mặc định?")) return;

        Settings.IsEnabled = true;
        Settings.ModelType = WhisperEnum.WhisperModelType.Base;
        Settings.UseGpu = false;
        Settings.GpuDeviceId = null;

        StateHasChanged();
        await SaveSettings();
    }

    private class ModelInfo
    {
        public WhisperEnum.WhisperModelType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Speed { get; set; } = string.Empty;
        public string Accuracy { get; set; } = string.Empty;
    }
}
