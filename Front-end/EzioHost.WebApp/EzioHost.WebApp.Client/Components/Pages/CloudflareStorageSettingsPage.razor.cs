using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Extensions;
using EzioHost.WebApp.Client.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EzioHost.WebApp.Client.Components.Pages;

[Authorize]
public partial class CloudflareStorageSettingsPage : IAsyncDisposable
{
    private IJSObjectReference? _jsModule;
    [Inject] public ICloudflareStorageSettingApi CloudflareStorageSettingApi { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    private CloudflareStorageSettingDto Settings { get; set; } = new();
    private bool IsLoading { get; set; } = true;
    private bool IsSaving { get; set; }
    private string? SuccessMessage { get; set; }
    private string? ErrorMessage { get; set; }

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
                    "./Components/Pages/CloudflareStorageSettingsPage.razor.js");
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
            Settings = await CloudflareStorageSettingApi.GetStorageSettings();
            ErrorMessage = null;
            SuccessMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải cài đặt: {ex.Message}";
            await JsRuntime.ShowErrorToast(ErrorMessage);
        }
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
            var updateDto = new CloudflareStorageSettingUpdateDto
            {
                IsEnabled = Settings.IsEnabled
            };

            Settings = await CloudflareStorageSettingApi.UpdateStorageSettings(updateDto);
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
        if (!await JsRuntime.Confirm("Bạn có chắc muốn đặt lại cài đặt về mặc định?")) return;

        Settings.IsEnabled = true;

        StateHasChanged();
        await SaveSettings();
    }
}
