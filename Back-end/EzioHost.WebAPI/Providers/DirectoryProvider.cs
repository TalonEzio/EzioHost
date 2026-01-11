using EzioHost.Core.Providers;

namespace EzioHost.WebAPI.Providers;

public class DirectoryProvider : IDirectoryProvider
{
    private readonly Lazy<string> _webRootPath = new(() =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        EnsureDirectoryExists(path);
        return path;
    });

    public string GetWebRootPath()
    {
        return _webRootPath.Value;
    }

    public string GetBaseUploadFolder()
    {
        return GetOrCreateSubFolder("Uploads");
    }

    public string GetThumbnailFolder()
    {
        return GetOrCreateSubFolder("Thumbnails");
    }

    public string GetBaseVideoFolder()
    {
        return GetOrCreateSubFolder("Videos");
    }

    public string GetOnnxModelFolder()
    {
        return GetOrCreateSubFolder("OnnxModels");
    }

    public string GetTempPath()
    {
        return GetOrCreateSubFolder("Temp");
    }

    public string GetOrCreateSubFolder(string folderName)
    {
        var path = Path.Combine(GetWebRootPath(), folderName);
        EnsureDirectoryExists(path);
        return path;
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }
}