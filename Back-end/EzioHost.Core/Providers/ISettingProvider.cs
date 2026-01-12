using EzioHost.Domain.Settings;

namespace EzioHost.Core.Providers;

public interface ISettingProvider
{
    VideoEncodeSettings GetVideoEncodeSettings();
    StorageSettings GetStorageSettings();
}