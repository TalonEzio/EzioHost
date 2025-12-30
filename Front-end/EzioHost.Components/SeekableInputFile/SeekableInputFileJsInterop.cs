using Microsoft.JSInterop;

namespace EzioHost.Components.SeekableInputFile;

public class SeekableInputFileJsInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() =>
        jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/EzioHost.Components/SeekableInputFile.razor.js")
            .AsTask());

    private bool IsWasm => OperatingSystem.IsBrowser();

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }

    public async ValueTask<string> Prompt(string message)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string>("showPrompt", message);
    }
}