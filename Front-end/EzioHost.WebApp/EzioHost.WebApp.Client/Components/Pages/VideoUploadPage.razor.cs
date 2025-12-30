using System.Net;
using EzioHost.Components.SeekableInputFile.Models;
using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Extensions;
using EzioHost.WebApp.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Refit;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class VideoUploadPage
{
    private DotNetObjectReference<VideoUploadPage>? _dotNetRef;
    private ElementReference? _dropZone;
    private IJSObjectReference? _jsModule;
    private long ChunkSize => AuthState!.UploadMaxSpeedMb * 1024;
    private long Storage => AuthState!.Storage * 1024 * 1024;

    [PersistentState] public AuthenticationState? AuthState { get; set; }

    [CascadingParameter] public Task<AuthenticationState> AuthStateTask { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public IUploadApi UploadApi { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    private List<BrowserFileStream> SelectedFiles { get; } = new();
    private bool IsUploading { get; set; }
    private int UploadProgress { get; set; }
    private string CurrentUploadFileName { get; set; } = "";
    private bool IsDragOver { get; set; }

    // Video settings
    private string VideoTitle { get; set; } = "";
    private string VideoDescription { get; set; } = "";

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_jsModule != null) await _jsModule.DisposeAsync();
            _dotNetRef?.Dispose();
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    protected override async Task OnInitializedAsync()
    {
        AuthState ??= await AuthStateTask;


        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _dropZone.HasValue)
        {
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import",
                "/Components/Pages/VideoUploadPage.razor.js");
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsModule.InvokeVoidAsync("setupDragAndDrop", _dropZone.Value, _dotNetRef);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    [JSInvokable]
    public void SetDragOver(bool isDragOver)
    {
        IsDragOver = isDragOver;
        StateHasChanged();
    }

    [JSInvokable]
    public async Task ShowWarning(string message)
    {
        await JsRuntime.ShowWarningToast(message);
    }

    private void OnFilesChanged(List<BrowserFileStream> files)
    {
        SelectedFiles.Clear();
        SelectedFiles.AddRange(files);
        StateHasChanged();
    }

    private void SelectFile(BrowserFileStream file)
    {
        VideoTitle = Path.GetFileNameWithoutExtension(file.Name);
        StateHasChanged();
    }

    private void RemoveFile(BrowserFileStream file)
    {
        SelectedFiles.Remove(file);
        if (SelectedFiles.Count == 0)
        {
            VideoTitle = "";
            VideoDescription = "";
        }

        StateHasChanged();
    }

    private async Task StartUpload()
    {
        if (!SelectedFiles.Any())
        {
            await JsRuntime.ShowWarningToast("Vui lòng chọn ít nhất một file");
            return;
        }

        IsUploading = true;
        UploadProgress = 0;
        StateHasChanged();

        try
        {
            foreach (var file in SelectedFiles)
            {
                CurrentUploadFileName = file.Name;
                StateHasChanged();

                await UploadFile(file);
            }

            await JsRuntime.ShowSuccessToast("Upload thành công!");
            await Task.Delay(1000);
            await JsRuntime.NavigateToAsync("/video");
        }
        catch (Exception e)
        {
            await JsRuntime.ShowErrorToast($"Upload lỗi: {e.Message}");
        }
        finally
        {
            IsUploading = false;
            UploadProgress = 0;
            CurrentUploadFileName = "";
            ClearFiles();
        }
    }

    private async Task UploadFile(BrowserFileStream file)
    {
        var uploadInfo = new UploadInfoDto
        {
            Id = Guid.NewGuid(),
            FileName = file.Name,
            FileSize = file.Size,
            ReceivedBytes = 0,
            ContentType = file.ContentType,
            Checksum = file.Checksum
        };

        var initResponse = await UploadApi.InitUpload(uploadInfo);
        if (!initResponse.IsSuccessStatusCode || initResponse.Content == null)
        {
            await JsRuntime.ShowErrorToast($"Không thể khởi tạo upload {file.Name}");
            return;
        }

        var uploadResult = initResponse.Content;

        if (initResponse.StatusCode == HttpStatusCode.Created && uploadResult.IsCompleted)
        {
            await JsRuntime.ShowInfoToast($"File '{file.Name}' đã được tải lên trước đó, đã copy hoàn tất!");
            return;
        }

        var uploadId = uploadResult.Id;
        var uploadedBytes = uploadResult.ReceivedBytes;

        await using var fileStream = file.OpenReadStream();
        var buffer = new byte[ChunkSize];

        while (uploadedBytes < file.Size)
        {
            var bytesToRead = (int)Math.Min(ChunkSize, file.Size - uploadedBytes);
            var bytesRead = await fileStream.ReadAsync(buffer, 0, bytesToRead);
            if (bytesRead == 0) break;

            var chunkData = new byte[bytesRead];
            Array.Copy(buffer, chunkData, bytesRead);

            using var chunkStream = new MemoryStream(chunkData);
            var streamPart = new StreamPart(chunkStream, file.Name, file.ContentType);

            try
            {
                await UploadApi.UploadChunk(uploadId, streamPart);
            }
            catch (ApiException ex)
            {
                throw new Exception($"Lỗi upload chunk: {ex.Message}");
            }

            uploadedBytes += bytesRead;

            var fileProgress = (int)(uploadedBytes * 100 / file.Size);
            var indexOf = SelectedFiles.IndexOf(file);
            var totalProgress = (indexOf * 100 + fileProgress) / SelectedFiles.Count;
            UploadProgress = totalProgress;
            StateHasChanged();
        }

        await Task.Delay(500);
    }

    private void ClearFiles()
    {
        SelectedFiles.Clear();
        VideoTitle = "";
        VideoDescription = "";
        StateHasChanged();
    }

    private async Task TriggerFileInput()
    {
        if (_jsModule != null) await _jsModule.InvokeVoidAsync("clickFileInput");
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}