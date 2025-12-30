using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace EzioHost.Components.SeekableInputFile.Models;

public class BrowserFile
{
    public Guid Id { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
}

public class BrowserFileStream(BrowserFile browserFile, IJSObjectReference jsModule, ILoggerFactory loggerFactory)
{
    public string Name => browserFile.Name;
    public long Size => browserFile.Size;
    public string ContentType => browserFile.ContentType;
    public string Checksum => browserFile.Checksum;

    public Stream OpenReadStream()
    {
        return new SeekableBrowserFileStream(jsModule, browserFile.Id, browserFile.Size, loggerFactory);
    }
}