using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Extensions;
using EzioHost.WebApp.Client.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EzioHost.WebApp.Client.Components.Pages;

[Authorize]
public partial class SettingsPage : IAsyncDisposable
{
    [Inject] public IEncodingQualitySettingApi EncodingApi { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    private List<EncodingQualitySettingDto>? EncodingSettings { get; set; }
    private IJSObjectReference? _jsModule;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Load encoding settings to show enabled count
            EncodingSettings = await EncodingApi.GetSettings();
        }
        catch (Exception ex)
        {
            // Silently fail - stats are optional
            Console.WriteLine($"Could not load encoding settings: {ex.Message}");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Pages/SettingsPage.razor.js");
            }
            catch
            {
                // Ignore if JS module doesn't exist
            }
        }
    }

    private int EnabledResolutionsCount
    {
        get
        {
            return EncodingSettings?.Count(s => s.IsEnabled) ?? 0;
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
}
