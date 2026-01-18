using EzioHost.Shared.Models;
using Refit;

namespace EzioHost.WebApp.Client.Services;

public interface ICloudflareStorageSettingApi
{
    [Get("/api/Video/storage-settings")]
    Task<CloudflareStorageSettingDto> GetStorageSettings();

    [Put("/api/Video/storage-settings")]
    Task<CloudflareStorageSettingDto> UpdateStorageSettings([Body] CloudflareStorageSettingUpdateDto dto);
}
