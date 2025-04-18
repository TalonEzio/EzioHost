﻿namespace EzioHost.Core.Providers
{
    public interface IDirectoryProvider
    {
        public string GetWebRootPath();
        public string GetBaseUploadFolder();
        public string GetBaseVideoFolder();
        public string GetOrCreateSubFolder(string folderName);

        public string GetOnnxModelFolder();

        public string GetTempPath();
    }
}
