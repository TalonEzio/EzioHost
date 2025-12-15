using System.Net.Http.Json;
using System.Text.Json;
using EzioHost.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class VideoSharePage
{
    private string _videoJsonData = string.Empty;
    [Parameter] public Guid VideoId { get; set; }

    [Inject] public IHttpClientFactory HttpClientFactory { get; set; } = null!;

    public VideoDto? Video { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var httpClient = HttpClientFactory.CreateClient(nameof(EzioHost));

        Video ??= await httpClient.GetFromJsonAsync<VideoDto>($"/api/video/{VideoId}");

        if (Video != null)
            _videoJsonData = JsonSerializer.Serialize(Video);
        else
            throw new Exception("Video not found");
    }
}