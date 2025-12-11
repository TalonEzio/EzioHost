using EzioHost.Core.Providers;
using EzioHost.Domain.Settings;
using EzioHost.WebAPI.Startup;
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
