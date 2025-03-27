using EzioHost.Core.Providers;

namespace EzioHost.WebAPI.Providers
{
    public class DirectoryProvider(IWebHostEnvironment webHostEnvironment) : IDirectoryProvider
    {
        public string GetWebRootPath()
        {
            return webHostEnvironment.WebRootPath;
        }
    }
}
