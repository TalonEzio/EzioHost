using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Extensions;
using System.Net.Http.Json;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class VideoUpscalePage
{
	[Parameter] public Guid VideoId { get; set; }
	[Inject] public IHttpClientFactory HttpClientFactory { get; set; } = null!;
	[Inject] public IJSRuntime JsRuntime { get; set; } = null!;
	public VideoDto? Video { get; set; }

	public List<OnnxModelDto> OnnxModels { get; set; } = [];
	public ImageCompare? ImageCompareComponent { get; set; }

	private Guid _selectedModelId;
	private OnnxModelDto? _selectedModel;

	protected override async Task OnInitializedAsync()
	{
		using var httpClient = HttpClientFactory.CreateClient(nameof(EzioHost));
		Video = await httpClient.GetFromJsonAsync<VideoDto>($"/api/Video/{VideoId}");

		OnnxModels = await httpClient.GetFromJsonAsync<List<OnnxModelDto>>("/api/OnnxModel?requireDemo=true") ?? [];
		if (Video == null)
		{
			throw new Exception($"Video Id {VideoId} not found");
		}

		if (!OnnxModels.Any())
		{
			return;
		}

		_selectedModel = OnnxModels.First();
		if (_selectedModel != null)
		{
			_selectedModelId = _selectedModel.Id;
		}
		StateHasChanged();
	}

	private Task OnOnnxModelChange(ChangeEventArgs eventArgs)
	{
		_selectedModelId = Guid.Parse(eventArgs.Value!.ToString() ?? string.Empty);
		_selectedModel = OnnxModels.FirstOrDefault(m => m.Id == _selectedModelId);
		StateHasChanged();

		if (_selectedModel != null && ImageCompareComponent != null)
		{
			ImageCompareComponent.UpdateImages(_selectedModel.DemoInput, _selectedModel.DemoOutput);
		}

		return Task.CompletedTask;
	}

	private async Task RequestUpscaleVideo()
	{
		try
		{
			using var httpClient = HttpClientFactory.CreateClient(nameof(EzioHost));
			var response = await httpClient.PostAsync($"/api/video/{VideoId}/upscale/{_selectedModelId}", null);

			response.EnsureSuccessStatusCode();
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

