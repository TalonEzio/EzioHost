using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Services;
using Microsoft.AspNetCore.Components;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class VideoEmbedPage
{
    [Parameter] public Guid VideoId { get; set; }

    [Inject] public IVideoApi VideoApi { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    public VideoDto? Video { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            Video ??= await VideoApi.GetVideoById(VideoId);
        }
        catch
        {
            NavigationManager.NavigateTo("/");
        }
    }
}