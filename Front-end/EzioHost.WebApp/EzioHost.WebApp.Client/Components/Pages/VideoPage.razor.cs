using EzioHost.Shared.Events;
using EzioHost.Shared.Extensions;
using EzioHost.Shared.HubActions;
using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Extensions;
using EzioHost.WebApp.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class VideoPage : IAsyncDisposable
{
    private HubConnection? _hubConnection;

    // Prevent re-entrant operations
    private bool _isDeleting;
    private bool _isUpdating;
    private IJSObjectReference? _jsObjectReference;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public IVideoApi VideoApi { get; set; } = null!;
    [Inject] public IAuthApi AuthApi { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    private List<VideoDto> Videos { get; set; } = [];
    private List<VideoDto> FilteredVideos { get; set; } = [];

    // UI State
    private bool IsLoading { get; set; } = true;
    private bool IsGridView { get; set; } = true;
    private VideoDto? SelectedVideo { get; set; }
    private VideoDto? EditingVideo { get; set; }

    // Filter and Search
    private string SearchTerm { get; set; } = "";
    private string SelectedShareType { get; set; } = "";
    private string SortBy { get; set; } = "created_desc";

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_hubConnection != null) await _hubConnection.DisposeAsync();
            if (_jsObjectReference != null) await _jsObjectReference.DisposeAsync();
        }
        catch
        {
            //ignore
        }
    }

    private void ApplyFilters()
    {
        var filtered = Videos.AsEnumerable();

        // Search filter
        if (!string.IsNullOrEmpty(SearchTerm))
            filtered = filtered.Where(v =>
                v.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

        // Share type filter
        if (!string.IsNullOrEmpty(SelectedShareType) && int.TryParse(SelectedShareType, out var shareType))
            filtered = filtered.Where(v => (int)v.ShareType == shareType);

        // Sort
        filtered = SortBy switch
        {
            "created_desc" => filtered.OrderByDescending(v => v.CreatedAt),
            "created_asc" => filtered.OrderBy(v => v.CreatedAt),
            "title_asc" => filtered.OrderBy(v => v.Title),
            "title_desc" => filtered.OrderByDescending(v => v.Title),
            _ => filtered.OrderByDescending(v => v.CreatedAt)
        };

        FilteredVideos = filtered.ToList();
    }

    private void OnFilterChanged()
    {
        ApplyFilters();
    }

    private void ToggleViewMode()
    {
        IsGridView = !IsGridView;
    }

    private string GetThumbnail(VideoDto video)
    {
        // Sử dụng thumbnail thực từ video, nếu không có thì dùng placeholder
        if (!string.IsNullOrEmpty(video.Thumbnail)) return video.Thumbnail;

        // Fallback về placeholder nếu không có thumbnail
        return $"https://via.placeholder.com/320x180/0d6efd/ffffff?text={Uri.EscapeDataString(video.Title)}";
    }

    private void ShowEditModal(VideoDto video)
    {
        EditingVideo = video;
    }

    private void CloseEditModal()
    {
        EditingVideo = null;
    }

    private void ClosePlayer()
    {
        SelectedVideo = null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsObjectReference ??=
                await JsRuntime.InvokeAsync<IJSObjectReference>("import", "/Components/Pages/VideoPage.razor.js");

            var url = await JsRuntime.GetOrigin();

            var hubUrl = Path.Combine(url, "hubs", "VideoHub").Replace("\\", "/");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl,
                    options => { options.AccessTokenProvider = async () => await AuthApi.GetAccessToken(); })
                .Build();

            _hubConnection.On<string>(nameof(IVideoHubAction.ReceiveMessage),
                async message => { await JsRuntime.ShowSuccessToast(message); });

            _hubConnection.On<VideoStreamAddedEventArgs>(nameof(IVideoHubAction.ReceiveNewVideoStream), async args =>
            {
                var video = Videos.FirstOrDefault(x => x.Id == args.VideoId);
                if (video == null) return;

                var videoStream = args.VideoStream;
                video.VideoStreams.Add(videoStream);
                video.VideoStreams = video.VideoStreams.DistinctBy(x => x.Id).ToList();

                ApplyFilters();
                await InvokeAsync(StateHasChanged);

                await JsRuntime.ShowSuccessToast(
                    $"Video {video.Title} đã xử lý xong {videoStream.Resolution.GetDescription()}");
            });

            _hubConnection.On<VideoProcessDoneEvent>(nameof(IVideoHubAction.ReceiveVideoProcessingDone), async args =>
            {
                var video = Videos.FirstOrDefault(x => x.Id == args.Video.Id);
                if (video == null)
                {
                    Videos.Add(args.Video);
                    ApplyFilters();
                    await InvokeAsync(StateHasChanged);
                    await JsRuntime.ShowSuccessToast($"Video {args.Video.Title} đã xử lý xong");
                    return;
                }

                video.VideoStreams.Clear();
                video.VideoStreams.AddRange(args.Video.VideoStreams);
                video.Status = args.Video.Status;
                video.VideoStreams = video.VideoStreams.DistinctBy(x => x.Id).ToList();

                ApplyFilters();
                await InvokeAsync(StateHasChanged);
                await JsRuntime.ShowSuccessToast($"Video {video.Title} đã xử lý xong");
            });
            try
            {
                await _hubConnection.StartAsync();
            }
            catch (Exception e)
            {
                await JsRuntime.ShowErrorToast($"Lỗi kết nối đến server: {e.Message}");
            }

            Videos = await VideoApi.GetVideos() ?? [];
            ApplyFilters();
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task PlayVideo(VideoDto video)
    {
        var origin = NavigationManager.BaseUri;

        SelectedVideo = video;
        StateHasChanged();
        await Task.Delay(100);

        if (_jsObjectReference != null)
        {
            await _jsObjectReference.InvokeVoidAsync("playVideo", "player", video.PlayerJsMetadata);
        }
    }

    private async Task UpdateVideo(VideoDto video)
    {
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            await VideoApi.UpdateVideo(new VideoUpdateDto
            {
                Id = video.Id,
                Title = video.Title,
                ShareType = video.ShareType
            });

            await JsRuntime.ShowSuccessToast("Cập nhật thành công");
            CloseEditModal();
            ApplyFilters();
        }
        catch (Exception e)
        {
            await JsRuntime.ShowErrorToast($"Cập nhật lỗi: {e.Message}.");
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private async Task DeleteVideo(VideoDto video)
    {
        if (_isDeleting) return;
        _isDeleting = true;

        try
        {
            var confirmed =
                await JsRuntime.InvokeAsync<bool>("confirm", $"Bạn có chắc chắn muốn xóa video '{video.Title}'?");
            if (!confirmed) return;

            await VideoApi.DeleteVideo(video.Id);

            Videos.Remove(video);
            ApplyFilters();
            await JsRuntime.ShowSuccessToast("Video đã xóa thành công");
        }
        catch (Exception e)
        {
            await JsRuntime.ShowErrorToast($"Xóa video lỗi: {e.Message}");
        }
        finally
        {
            _isDeleting = false;
        }
    }

    private async Task HandleSubtitleChanged()
    {
        if (EditingVideo != null)
        {
            // Reload video to get updated subtitles
            var updatedVideo = await VideoApi.GetVideoById(EditingVideo.Id);

            var index = Videos.FindIndex(v => v.Id == updatedVideo.Id);
            if (index >= 0)
            {
                Videos[index] = updatedVideo;
                if (EditingVideo.Id == updatedVideo.Id)
                {
                    EditingVideo = updatedVideo;
                }
            }
            ApplyFilters();

        }
    }
}