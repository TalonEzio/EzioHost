using EzioHost.Domain.Settings;

namespace EzioHost.Core.Providers;

public interface ISettingProvider
{
    VideoEncodeSetting GetVideoEncodeSetting();
}