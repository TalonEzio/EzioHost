using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EzioHost.WebApp.Client.Components.Pages;

public partial class VideoSharePage : IAsyncDisposable
{
	[Parameter] public Guid VideoId { get; set; }
	[Inject] public IJSRuntime JsRuntime { get; set; } = null!;

	private IJSObjectReference? _jsObjectReference;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_jsObjectReference = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "/Components/Pages/VideoSharePage.razor.js");
			await _jsObjectReference.InvokeVoidAsync("loadVideo", VideoId.ToString());
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (_jsObjectReference != null)
		{
			await _jsObjectReference.DisposeAsync();
		}
	}
}

