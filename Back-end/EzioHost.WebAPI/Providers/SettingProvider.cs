using EzioHost.Core.Providers;
using EzioHost.Domain.Settings;
using Microsoft.Extensions.Options;

namespace EzioHost.WebAPI.Providers
{
    public class SettingProvider(IOptionsMonitor<AppSettings> appSettingsMonitor) : ISettingProvider
    {
        public VideoEncodeSetting GetVideoEncodeSetting()
        {
            return appSettingsMonitor.CurrentValue.VideoEncode;
        }
    }
}
