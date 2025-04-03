using EzioHost.Core.Providers;

namespace EzioHost.WebAPI.Providers
{
    public class DirectoryProvider(IWebHostEnvironment webHostEnvironment) : IDirectoryProvider
    {
        public string GetWebRootPath()
        {
            return webHostEnvironment.WebRootPath;
        }

        public string GetBaseUploadFolder() => GetSubFolderFromWebRoot("Uploads");
        public string GetBaseVideoFolder() => GetSubFolderFromWebRoot("Videos");
        public string GetOnnxModelFolder() => GetSubFolderFromWebRoot("OnnxModels");
        public string GetTempPath()
        {
            return Path.GetTempPath();
        }

        public string GetSubFolderFromWebRoot(string folderName)
        {
            var result = Path.Combine(GetWebRootPath(), folderName);
            if (!Directory.Exists(result))
            {
                Directory.CreateDirectory(result);
            }

            return result;
        }


    }
}
