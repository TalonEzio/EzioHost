using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Extensions;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.Authorization;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class UploadVideoPage
{
    [CascadingParameter] public Task<AuthenticationState> AuthStateTask { get; set; } = null!;

    private AuthenticationState _authState = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public IHttpClientFactory HttpClientFactory { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    private const string FILE_INPUT_ID = "fileInput";
    private ElementReference? _dropZone;
    private List<IBrowserFile> SelectedFiles { get; set; } = new();
    private bool IsDragOver { get; set; }
    private bool IsUploading { get; set; }
    private int UploadProgress { get; set; }
    private string CurrentUploadFileName { get; set; } = "";

    // Video settings
    private string VideoTitle { get; set; } = "";
    private string VideoDescription { get; set; } = "";
    private string TargetQuality { get; set; } = "1080p";
    private int SelectedVideoType { get; set; } = (int)VideoEnum.VideoType.Other;

    private IJSObjectReference? _jsObjectReference;
    private DotNetObjectReference<UploadVideoPage>? _dotNetRef;

    private const int CHUNK_SIZE = 1024 * 1024; // 1MB chunks


    protected override async Task OnInitializedAsync()
    {
        _authState = await AuthStateTask;
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsObjectReference ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", "/Components/Pages/UploadVideoPage.razor.js");
            _dotNetRef = DotNetObjectReference.Create(this);
            if (_jsObjectReference != null && _dotNetRef != null && _dropZone.HasValue)
            {
                await _jsObjectReference.InvokeVoidAsync("setupDragAndDrop", _dropZone.Value, FILE_INPUT_ID, _dotNetRef);
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsObjectReference != null)
        {
            await _jsObjectReference.DisposeAsync();
        }
        _dotNetRef?.Dispose();
    }

    private async Task TriggerFileInput()
    {
        if (_jsObjectReference != null)
        {
            await _jsObjectReference.InvokeVoidAsync("clickFileInputById", FILE_INPUT_ID);
        }
    }

    private async Task<int> FindFileIndexInInput(IBrowserFile file)
    {
        if (_jsObjectReference == null)
        {
            return -1;
        }

        return await _jsObjectReference.InvokeAsync<int>("findFileIndex", FILE_INPUT_ID, file.Name, file.Size);
    }

    private Task OnFileSelected(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles())
        {
            if (!SelectedFiles.Any(f => f.Name == file.Name && f.Size == file.Size))
            {
                SelectedFiles.Add(file);
            }
        }

        if (SelectedFiles.Count == 1 && string.IsNullOrEmpty(VideoTitle))
        {
            VideoTitle = Path.GetFileNameWithoutExtension(SelectedFiles[0].Name);
        }
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void OnDragOver(DragEventArgs e)
    {
        IsDragOver = true;
    }

    private void OnDragLeave(DragEventArgs e)
    {
        IsDragOver = false;
    }

    [JSInvokable]
    public void SetDragOver(bool isDragOver)
    {
        IsDragOver = isDragOver;
        StateHasChanged();
    }

    private void RemoveFile(IBrowserFile file)
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

        if (string.IsNullOrWhiteSpace(VideoTitle))
        {
            await JsRuntime.ShowWarningToast("Vui lòng nhập tiêu đề video");
            return;
        }

        IsUploading = true;
        UploadProgress = 0;
        StateHasChanged();

        try
        {
            var userId = _authState.UserId;
            foreach (var file in SelectedFiles)
            {
                CurrentUploadFileName = file.Name;
                StateHasChanged();

                // Find file index in input element
                var fileIndex = await FindFileIndexInInput(file);
                if (fileIndex < 0)
                {
                    await JsRuntime.ShowErrorToast($"Không tìm thấy file {file.Name} trong input element");
                    continue;
                }

                await UploadFile(file, fileIndex, userId);
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

    private async Task UploadFile(IBrowserFile file, int fileIndex, Guid userId)
    {
        using var httpClient = HttpClientFactory.CreateClient(nameof(EzioHost));

        // For large files, skip checksum calculation to avoid reading file twice
        // Checksum is optional and can be calculated on server side if needed
        string? checksum = null;

        // Only calculate checksum for files smaller than 100MB
        if (file.Size < 100 * 1024 * 1024)
        {
            checksum = await CalculateFileChecksum(file, fileIndex);
        }

        // Initialize upload
        var uploadInfo = new UploadInfoDto
        {
            FileName = file.Name,
            FileSize = file.Size,
            ContentType = file.ContentType,
            Checksum = checksum,
            UserId = userId,
            Type = (VideoEnum.VideoType)SelectedVideoType
        };

        var initResponse = await httpClient.PostAsJsonAsync("api/Upload/init", uploadInfo);
        initResponse.EnsureSuccessStatusCode();

        var uploadResult = await initResponse.Content.ReadFromJsonAsync<UploadInfoDto>();
        if (uploadResult == null)
        {
            throw new Exception("Không thể khởi tạo upload");
        }

        var uploadId = uploadResult.Id;
        var uploadedBytes = 0L;

        // Upload chunks using SeekableFileStream
        await using var fileStream = new SeekableFileStream(JsRuntime, FILE_INPUT_ID, fileIndex, file.Size);
        var buffer = new byte[CHUNK_SIZE];

        while (uploadedBytes < file.Size)
        {
            var bytesToRead = (int)Math.Min(CHUNK_SIZE, file.Size - uploadedBytes);
            var bytesRead = await fileStream.ReadAsync(buffer, 0, bytesToRead);
            if (bytesRead == 0) break;

            var chunkData = new byte[bytesRead];
            Array.Copy(buffer, chunkData, bytesRead);

            using var formData = new MultipartFormDataContent();
            using var chunkStream = new MemoryStream(chunkData);
            using var chunkContent = new StreamContent(chunkStream);
            chunkContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            formData.Add(chunkContent, "chunkFile", file.Name);

            var chunkResponse = await httpClient.PostAsync($"api/Upload/chunk/{uploadId}", formData);

            if (!chunkResponse.IsSuccessStatusCode)
            {
                var errorMessage = await chunkResponse.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi upload chunk: {errorMessage}");
            }

            uploadedBytes += bytesRead;

            // Update progress
            var fileProgress = (int)((uploadedBytes * 100) / file.Size);
            var indexOf = SelectedFiles.IndexOf(file);
            var totalProgress = ((indexOf * 100) + fileProgress) / SelectedFiles.Count;
            UploadProgress = totalProgress;
            StateHasChanged();
        }

        // Wait a bit for server to process
        await Task.Delay(500);
    }

    private async Task<string?> CalculateFileChecksum(IBrowserFile file, int fileIndex)
    {
        try
        {
            await using var stream = new SeekableFileStream(JsRuntime, FILE_INPUT_ID, fileIndex, file.Size);
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not calculate checksum: {ex.Message}");
            return null;
        }
    }

    private void ClearFiles()
    {
        SelectedFiles.Clear();
        VideoTitle = "";
        VideoDescription = "";
        StateHasChanged();
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
