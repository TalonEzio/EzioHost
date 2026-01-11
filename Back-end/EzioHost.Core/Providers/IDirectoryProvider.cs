namespace EzioHost.Core.Providers;

public interface IDirectoryProvider
{
    public string GetWebRootPath();
    public string GetBaseUploadFolder();
    public string GetThumbnailFolder();
    public string GetBaseVideoFolder();
    public string GetOnnxModelFolder();
    public string GetTempPath();
}