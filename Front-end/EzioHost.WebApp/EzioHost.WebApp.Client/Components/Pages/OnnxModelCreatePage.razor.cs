using EzioHost.Shared.Constants;
using EzioHost.Shared.Models;
using EzioHost.WebApp.Client.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class OnnxModelCreatePage
{
    private IBrowserFile? _file;
    private bool _isSubmitting;
    private IJSObjectReference? _jsObjectReference;
    [Inject] public IHttpClientFactory HttpClientFactory { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;

    [SupplyParameterFromForm] public OnnxModelCreateDto Onnx { get; set; } = null!;

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
            var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(Onnx.Name), nameof(OnnxModelCreateDto.Name));
            formData.Add(new StringContent(Onnx.Scale.ToString()), nameof(OnnxModelCreateDto.Scale));
            formData.Add(new StringContent(Onnx.MustInputWidth.ToString()), nameof(OnnxModelCreateDto.MustInputWidth));
            formData.Add(new StringContent(Onnx.MustInputHeight.ToString()),
                nameof(OnnxModelCreateDto.MustInputHeight));
            formData.Add(new StringContent(Onnx.Precision.ToString()), nameof(OnnxModelCreateDto.Precision));

            using var httpClient = HttpClientFactory.CreateClient(nameof(EzioHost));
            var fileContent = new StreamContent(_file.OpenReadStream(_file.Size));
            formData.Add(fileContent, FormFieldNames.ModelFile, _file.Name);

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