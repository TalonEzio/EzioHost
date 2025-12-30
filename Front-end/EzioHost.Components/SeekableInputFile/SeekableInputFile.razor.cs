using EzioHost.Components.SeekableInputFile.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace EzioHost.Components.SeekableInputFile;

public partial class SeekableInputFile : IAsyncDisposable
{
    private ElementReference _fileInput;
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<SeekableInputFile>? _objRef;

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> InputAttributes { get; set; } = new();

    [Parameter] public EventCallback<List<BrowserFileStream>> OnChanged { get; set; }

    [Inject] public ILogger<SeekableInputFile> Logger { get; set; } = null!;

    [Inject] public ILoggerFactory LoggerFactory { get; set; } = null!;

    public List<BrowserFileStream> Files { get; } = new();

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_jsModule != null) await _jsModule.DisposeAsync();

            _objRef?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error disposing SeekableInputFile");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import",
                "/_content/EzioHost.Components/SeekableInputFile/SeekableInputFile.razor.js");

            _objRef = DotNetObjectReference.Create(this);

            await _jsModule.InvokeVoidAsync("listenFilesSelected", _fileInput, _objRef);
        }
    }

    [JSInvokable]
    public async Task OnFileSelected(List<BrowserFile> browserFiles)
    {
        Files.Clear();
        foreach (var browserFile in browserFiles)
            Files.Add(new BrowserFileStream(browserFile, _jsModule!, LoggerFactory));

#if DEBUG
        Logger.LogInformation("Received {Count} files, Total size: {Size}", Files.Count, Files.Sum(x => x.Size));
#endif

        StateHasChanged();

        if (OnChanged.HasDelegate) await OnChanged.InvokeAsync(Files);
    }
}