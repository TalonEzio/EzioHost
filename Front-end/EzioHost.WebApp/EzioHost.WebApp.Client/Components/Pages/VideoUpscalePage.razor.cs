using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Extensions;
using EzioHost.WebApp.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class VideoUpscalePage
{
    private OnnxModelDto? _selectedModel;
    private bool _shouldRedirectToOnnx;
    private Guid _selectedModelId;
    [Parameter] public Guid VideoId { get; set; }
    [Inject] public IVideoApi VideoApi { get; set; } = null!;
    [Inject] public IOnnxModelApi OnnxModelApi { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    public VideoDto? Video { get; set; }

    public List<OnnxModelDto> OnnxModels { get; set; } = [];
    public ImageCompare? ImageCompareComponent { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Video = await VideoApi.GetVideoById(VideoId);
        if (Video == null) throw new Exception($"Video Id {VideoId} not found");

        var allModels = await OnnxModelApi.GetOnnxModels(true) ?? [];
        
        OnnxModels = allModels.Where(m => m.CanPreview).ToList();

        if (!OnnxModels.Any())
        {
            _shouldRedirectToOnnx = true;
            return;
        }

        _selectedModel = OnnxModels.First();
        if (_selectedModel != null) _selectedModelId = _selectedModel.Id;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _shouldRedirectToOnnx)
        {
            await JsRuntime.ShowWarningToast("Không có mô hình AI nào có preview. Vui lòng tạo preview trước khi upscale video.");
            await Task.Delay(2000);
            await JsRuntime.NavigateToAsync("/onnx");
        }
    }

    private Task OnOnnxModelChange(ChangeEventArgs eventArgs)
    {
        _selectedModelId = Guid.Parse(eventArgs.Value!.ToString() ?? string.Empty);
        _selectedModel = OnnxModels.FirstOrDefault(m => m.Id == _selectedModelId);
        StateHasChanged();

        if (_selectedModel != null && ImageCompareComponent != null)
            ImageCompareComponent.UpdateImages(_selectedModel.DemoInput, _selectedModel.DemoOutput);

        return Task.CompletedTask;
    }

    private async Task RequestUpscaleVideo()
    {
        try
        {
            await VideoApi.UpscaleVideo(VideoId, _selectedModelId);
            await JsRuntime.ShowSuccessToast("Đã thêm vào hàng đợi upscale thành công!");
            await Task.Delay(3000);

            await JsRuntime.NavigateToAsync("/video");
        }
        catch (Exception e)
        {
            await JsRuntime.ShowErrorToast($"Lỗi upscale :{e.Message}");
        }
    }
}