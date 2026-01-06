using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Extensions;
using EzioHost.WebApp.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Refit;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class OnnxModelCreatePage
{
    private IBrowserFile? _file;
    private bool _isCheckingOnnxModel;
    private bool _isSubmitting;
    private IJSObjectReference? _jsObjectReference;
    [Inject] public IOnnxModelApi OnnxModelApi { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;

    public OnnxModelCreateDto Onnx { get; set; } = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.InvokeAsync<IJSObjectReference>("import", "https://cdn.jsdelivr.net/npm/tom-select");
            _jsObjectReference =
                await JsRuntime.InvokeAsync<IJSObjectReference>("import",
                    "/Components/Pages/OnnxModelCreatePage.razor.js");
        }
    }

    private async Task CreateNewOnnxModelSubmit()
    {
        if (_file == null)
        {
            await JsRuntime.ShowErrorToast("Please upload an ONNX model file!");
            return;
        }

        _isSubmitting = true;
        StateHasChanged();

        try
        {
            var fileStream = _file.OpenReadStream(_file.Size);
            var streamPart = new StreamPart(fileStream, _file.Name, _file.ContentType);

            await OnnxModelApi.AddOnnxModel(
                Onnx.Name,
                Onnx.Scale,
                Onnx.MustInputWidth,
                Onnx.MustInputHeight,
                (int)Onnx.ElementType,
                streamPart);

            await JsRuntime.ShowSuccessToast("ONNX model created successfully!");

            await Task.Delay(1500);
            await JsRuntime.NavigateToAsync("/onnx");
        }
        catch (Exception e)
        {
            await JsRuntime.ShowErrorToast($"Failed to create model: {e.Message}");
            _isSubmitting = false;
            StateHasChanged();
        }
    }

    private async Task OnFileChanged(InputFileChangeEventArgs arg)
    {
        _file = arg.File;
        StateHasChanged();

        // Analyze ONNX model to auto-fill metadata
        if (_file != null && _file.Name.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase))
            try
            {
                _isCheckingOnnxModel = true;
                await using var fileStream = _file.OpenReadStream(_file.Size);
                var streamPart = new StreamPart(fileStream, _file.Name, _file.ContentType);

                var metadata = await OnnxModelApi.AnalyzeOnnxModel(streamPart);

                if (!string.IsNullOrEmpty(metadata.ErrorMessage))
                {
                    await JsRuntime.ShowWarningToast($"Không thể đọc metadata: {metadata.ErrorMessage}");
                }
                else
                {
                    // Auto-fill form with detected values
                    if (metadata.Scale.HasValue && Onnx.Scale == 1) // Only override if user hasn't changed default
                        Onnx.Scale = metadata.Scale.Value;
                    else
                        Onnx.Scale = 0;

                    if (metadata.MustInputWidth.HasValue && Onnx.MustInputWidth == 0)
                        Onnx.MustInputWidth = metadata.MustInputWidth.Value;
                    else
                        Onnx.MustInputWidth = 0;

                    if (metadata.MustInputHeight.HasValue && Onnx.MustInputHeight == 0)
                        Onnx.MustInputHeight = metadata.MustInputHeight.Value;
                    else
                        Onnx.MustInputHeight = 0;

                    Onnx.ElementType = metadata.ElementType;

                    await JsRuntime.ShowSuccessToast("Đã tự động phát hiện thông tin model");
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                await JsRuntime.ShowWarningToast($"Lỗi khi phân tích model: {ex.Message}");
            }
            finally
            {
                _isCheckingOnnxModel = false;
                StateHasChanged();
            }
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}