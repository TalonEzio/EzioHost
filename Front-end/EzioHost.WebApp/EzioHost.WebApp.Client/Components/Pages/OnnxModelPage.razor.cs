using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Extensions;
using EzioHost.WebApp.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Refit;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class OnnxModelPage
{
    private string _afterImage = string.Empty;
    private string _beforeImage = string.Empty;

    private string _message = "Waiting for data...";
    private IBrowserFile? _selectedDemoInputFile;

    private OnnxModelDto? _selectedOnnxModelDto;

    private bool _upscaling;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public IOnnxModelApi OnnxModelApi { get; set; } = null!;
    public ImageCompare? ImageCompare { get; set; }

    [PersistentState] public List<OnnxModelDto>? OnnxModels { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (OnnxModels is null)
        {
            OnnxModels = [];
            var response = await OnnxModelApi.GetOnnxModels();

            if (response.Any())
                OnnxModels.AddRange(response);
            else
                _message = "No ONNX model!";
        }
    }

    private Task OnPreviewClicked(OnnxModelDto onnxModel)
    {
        _selectedOnnxModelDto = onnxModel;

        // Nếu model đã có demo preview, hiển thị nó
        if (onnxModel.CanPreview)
        {
            _beforeImage = onnxModel.DemoInput;
            _afterImage = onnxModel.DemoOutput;

            if (ImageCompare != null) ImageCompare.UpdateImages(_beforeImage, _afterImage);
        }
        else
        {
            // Nếu chưa có preview, clear images để hiển thị form upload
            _beforeImage = string.Empty;
            _afterImage = string.Empty;
        }

        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task OnDeleteModelClicked(OnnxModelDto onnxModel)
    {
        await OnnxModelApi.DeleteOnnxModel(onnxModel.Id);

        await JsRuntime.ShowSuccessToast($"Xóa {onnxModel.Name} thành công!");

        OnnxModels?.Remove(onnxModel);
        StateHasChanged();
    }

    private Task OnDemoInputFileChanged(InputFileChangeEventArgs arg)
    {
        _selectedDemoInputFile = arg.File;
        return Task.CompletedTask;
    }

    private async Task OnTestUpscaleClicked(IBrowserFile? inputFile, OnnxModelDto? onnxModel)
    {
        if (inputFile == null || onnxModel == null)
        {
            await JsRuntime.ShowErrorToast("Please select an image file!");
            return;
        }

        _upscaling = true;
        StateHasChanged();

        try
        {
            var imageStream = inputFile.OpenReadStream(inputFile.Size);
            var streamPart = new StreamPart(imageStream, inputFile.Name, inputFile.ContentType);

            var content = await OnnxModelApi.DemoUpscale(onnxModel.Id, streamPart);


            _beforeImage = content.DemoInput;
            _afterImage = content.DemoOutput;

            var model = OnnxModels?.FirstOrDefault(x => x.Id == content.ModelId);
            if (model != null)
            {
                model.DemoInput = content.DemoInput;
                model.DemoOutput = content.DemoOutput;
            }

            // Reset selected file sau khi upscale thành công
            _selectedDemoInputFile = null;

            ImageCompare!.UpdateImages(_beforeImage, _afterImage);
            await JsRuntime.ShowSuccessToast(
                $"Upscale completed successfully! Time: {content.ElapsedMilliseconds}ms");
            StateHasChanged();
        }
        catch (Exception e)
        {
            await JsRuntime.ShowErrorToast($"Upscale failed: {e.Message}");
        }
        finally
        {
            _upscaling = false;
            StateHasChanged();
        }
    }

    private async Task OnRemovePreviewClicked(OnnxModelDto onnxModel)
    {
        try
        {
            onnxModel.DemoInput = onnxModel.DemoOutput = string.Empty;
            _beforeImage = _afterImage = string.Empty;

            await OnnxModelApi.ResetDemo(onnxModel.Id);

            await JsRuntime.ShowSuccessToast($"Xóa preview cho {onnxModel.Name} thành công!");
            StateHasChanged();
        }
        catch (Exception e)
        {
            await JsRuntime.ShowErrorToast($"Xóa preview lỗi, vui long thử lại. {e.Message}");
        }
    }
}