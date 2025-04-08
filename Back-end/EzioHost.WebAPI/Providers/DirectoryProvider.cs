using EzioHost.Core.Providers;

namespace EzioHost.WebAPI.Providers
{
    public class DirectoryProvider : IDirectoryProvider
    {
        private readonly Lazy<string> _webRootPath = new(() =>
        {
            var path = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            EnsureDirectoryExists(path);
            return path;
        });

        public string GetWebRootPath() => _webRootPath.Value;

        public string GetBaseUploadFolder() => GetOrCreateSubFolder("Uploads");
        public string GetBaseVideoFolder() => GetOrCreateSubFolder("Videos");

        public string GetOnnxModelFolder() => GetOrCreateSubFolder("OnnxModels");
        public string GetTempPath() => GetOrCreateSubFolder("Temp");

        public string GetOrCreateSubFolder(string folderName)
        {
            var path = Path.Combine(GetWebRootPath(), folderName);
            EnsureDirectoryExists(path);
            return path;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}