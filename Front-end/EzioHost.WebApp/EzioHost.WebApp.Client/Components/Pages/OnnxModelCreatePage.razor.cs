using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Extensions;
using Microsoft.JSInterop;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class OnnxModelCreatePage
{
	[Inject] public IHttpClientFactory HttpClientFactory { get; set; } = null!;
	[Inject] public IJSRuntime JsRuntime { get; set; } = null!;

	[SupplyParameterFromForm] public OnnxModelCreateDto Onnx { get; set; } = new();
	private IJSObjectReference? _jsObjectReference;

	private IBrowserFile? _file;
	private bool _isSubmitting;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await JsRuntime.InvokeAsync<IJSObjectReference>("import", "https://cdn.jsdelivr.net/npm/tom-select");
			_jsObjectReference = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "/Components/Pages/OnnxModelCreatePage.razor.js");
			await _jsObjectReference.InvokeVoidAsync("initTomSelect", "videoTypeSelect");
		}
	}

	private async Task<List<VideoEnum.VideoType>> GetSelectedVideoTypes()
	{
		if (_jsObjectReference == null) return [];
		var selectedValues = await _jsObjectReference.InvokeAsync<int[]>("getSelectedValues", "videoTypeSelect");
		var selectedTypes = selectedValues.Select(v => (VideoEnum.VideoType)v).ToList();
		return selectedTypes;
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
			var selected = await GetSelectedVideoTypes();
			Onnx.SupportVideoType = selected.Aggregate(VideoEnum.VideoType.None, (current, type) => current | type);

			var formData = new MultipartFormDataContent();

			formData.Add(new StringContent(Onnx.Name), "Name");
			formData.Add(new StringContent(Onnx.Scale.ToString()), "Scale");
			formData.Add(new StringContent(Onnx.MustInputWidth.ToString()), "MustInputWidth");
			formData.Add(new StringContent(Onnx.MustInputHeight.ToString()), "MustInputHeight");
			formData.Add(new StringContent(Onnx.SupportVideoType.ToString()), "SupportVideoType");
			formData.Add(new StringContent(Onnx.Precision.ToString()), "Precision");

			using var httpClient = HttpClientFactory.CreateClient(nameof(EzioHost));
			var fileContent = new StreamContent(_file.OpenReadStream(_file.Size));
			formData.Add(fileContent, "modelFile", _file.Name);

			var response = await httpClient.PutAsync("/api/OnnxModel", formData);
			response.EnsureSuccessStatusCode();

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

	private Task OnFileChanged(InputFileChangeEventArgs arg)
	{
		_file = arg.File;
		StateHasChanged();
		return Task.CompletedTask;
	}

	private string FormatFileSize(long bytes)
	{
		string[] sizes = { "B", "KB", "MB", "GB" };
		double len = bytes;
		int order = 0;
		while (len >= 1024 && order < sizes.Length - 1)
		{
			order++;
			len = len / 1024;
		}
		return $"{len:0.##} {sizes[order]}";
	}
}
