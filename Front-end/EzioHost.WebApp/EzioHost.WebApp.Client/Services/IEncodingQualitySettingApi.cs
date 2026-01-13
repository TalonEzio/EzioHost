using EzioHost.Shared.Models;
using Refit;

namespace EzioHost.WebApp.Client.Services;

public interface IEncodingQualitySettingApi
{
    [Get("/api/EncodingQualitySetting")]
    Task<List<EncodingQualitySettingDto>> GetSettings();

    [Put("/api/EncodingQualitySetting")]
    Task<List<EncodingQualitySettingDto>> UpdateSettings([Body] EncodingQualitySettingUpdateRequest request);
}