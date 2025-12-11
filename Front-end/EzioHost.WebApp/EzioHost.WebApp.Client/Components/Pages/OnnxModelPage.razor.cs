using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Extensions;
using System.Net.Http.Json;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class OnnxModelPage
{
	[Inject] public IJSRuntime JsRuntime { get; set; } = null!;
	[Inject] public IHttpClientFactory HttpClientFactory { get; set; } = null!;
	public ImageCompare? ImageCompare { get; set; }

	public List<OnnxModelDto> OnnxModels { get; set; } = [];

	private string _message = "Waiting for data...";
	private string _beforeImage = string.Empty;
	private string _afterImage = string.Empty;

	bool _upscaling;

	private OnnxModelDto? _selectedOnnxModelDto;
	private IBrowserFile? _selectedDemoInputFile;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			using var httpClient = HttpClientFactory.CreateClient(nameof(EzioHost));
			var response = await httpClient.GetFromJsonAsync<List<OnnxModelDto>>("api/OnnxModel");

			if (response != null && response.Any())
			{
				OnnxModels.AddRange(response);
			}
			else
			{
				_message = "No ONNX model!";
			}

			StateHasChanged();
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

			if (ImageCompare != null)
			{
				ImageCompare.UpdateImages(_beforeImage, _afterImage);
			}
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
		using var httpClient = HttpClientFactory.CreateClient(nameof(EzioHost));
		var response = await httpClient.DeleteAsync($"/api/OnnxModel/{onnxModel.Id}");

		response.EnsureSuccessStatusCode();

		await JsRuntime.ShowSuccessToast($"Xóa {onnxModel.Name} thành công!");

		OnnxModels.Remove(onnxModel);
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
			using var httpClient = HttpClientFactory.CreateClient(nameof(EzioHost));
			var formContent = new MultipartFormDataContent();
			var imageStream = inputFile.OpenReadStream(inputFile.Size);
			formContent.Add(new StreamContent(imageStream), "imageFile", inputFile.Name);

			var response = await httpClient.PostAsync($"/api/OnnxModel/demo/{onnxModel.Id}", formContent);
			response.EnsureSuccessStatusCode();

			var content = await response.Content.ReadFromJsonAsync<UpscaleDemoResponseDto>();

			if (content != null)
			{
				_beforeImage = content.DemoInput;
				_afterImage = content.DemoOutput;

				var model = OnnxModels.FirstOrDefault(x => x.Id == content.ModelId);
				if (model != null)
				{
					model.DemoInput = content.DemoInput;
					model.DemoOutput = content.DemoOutput;
				}

				// Reset selected file sau khi upscale thành công
				_selectedDemoInputFile = null;

				await JsRuntime.ShowSuccessToast($"Upscale completed successfully! Time: {content.ElapsedMilliseconds}ms");
				StateHasChanged();
			}
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

			using var httpClient = HttpClientFactory.CreateClient(nameof(EzioHost));
			var response = await httpClient.PostAsync($"/api/OnnxModel/demo-reset/{onnxModel.Id}", null);

			response.EnsureSuccessStatusCode();
			await JsRuntime.ShowSuccessToast($"Xóa preview cho {onnxModel.Name} thành công!");
			StateHasChanged();
		}
		catch (Exception e)
		{
			await JsRuntime.ShowErrorToast($"Xóa preview lỗi, vui long thử lại. {e.Message}");
		}
	}
}

