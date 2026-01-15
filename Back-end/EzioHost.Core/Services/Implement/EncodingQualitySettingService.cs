using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Models;

namespace EzioHost.Core.Services.Implement;

public class EncodingQualitySettingService(
    IEncodingQualitySettingRepository repository) : IEncodingQualitySettingService
{
    private static readonly Dictionary<VideoEnum.VideoResolution, int> DefaultBitrates = new()
    {
        { VideoEnum.VideoResolution._144p, 400 },
        { VideoEnum.VideoResolution._240p, 600 },
        { VideoEnum.VideoResolution._360p, 800 },
        { VideoEnum.VideoResolution._480p, 1400 },
        { VideoEnum.VideoResolution._720p, 2800 },
        { VideoEnum.VideoResolution._960p, 4000 },
        { VideoEnum.VideoResolution._1080p, 5000 },
        { VideoEnum.VideoResolution._1440p, 8000 },
        { VideoEnum.VideoResolution._1920p, 8000 },
        { VideoEnum.VideoResolution._2160p, 15000 }
    };

    public async Task<List<EncodingQualitySettingDto>> GetUserSettings(Guid userId)
    {
        var hasSettings = await repository.UserHasSettings(userId);

        if (!hasSettings) await CreateDefaultSettings(userId);

        var settings = await repository.GetSettingsByUserId(userId);

        return settings
            .OrderBy(s => (int)s.Resolution)
            .Select(s => new EncodingQualitySettingDto
            {
                Id = s.Id,
                Resolution = s.Resolution,
                BitrateKbps = s.BitrateKbps,
                IsEnabled = s.IsEnabled
            })
            .ToList();
    }

    public async Task<List<EncodingQualitySettingDto>> UpdateUserSettings(Guid userId,
        EncodingQualitySettingUpdateRequest request)
    {
        var existingSettings = (await repository.GetSettingsByUserId(userId)).ToList();
        var existingSettingsDict = existingSettings.ToDictionary(s => s.Resolution);

        var updatedSettings = new List<EncodingQualitySetting>();

        foreach (var item in request.Settings)
            if (item.Id.HasValue && existingSettingsDict.TryGetValue(item.Resolution, out var existing))
            {
                // Update existing setting
                existing.BitrateKbps = item.BitrateKbps;
                existing.IsEnabled = item.IsEnabled;
                existing.ModifiedAt = DateTime.UtcNow;
                existing.ModifiedBy = userId;
                updatedSettings.Add(await repository.UpdateSetting(existing));
            }
            else
            {
                // Create new setting
                var newSetting = new EncodingQualitySetting
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Resolution = item.Resolution,
                    BitrateKbps = item.BitrateKbps,
                    IsEnabled = item.IsEnabled,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };
                updatedSettings.Add(await repository.AddSetting(newSetting));
            }

        // Delete settings that are not in the request (optional - depends on requirements)
        // For now, we'll keep all settings and just update/add what's in the request

        return updatedSettings
            .OrderBy(s => (int)s.Resolution)
            .Select(s => new EncodingQualitySettingDto
            {
                Id = s.Id,
                Resolution = s.Resolution,
                BitrateKbps = s.BitrateKbps,
                IsEnabled = s.IsEnabled
            })
            .ToList();
    }

    public async Task<List<EncodingQualitySettingDto>> GetActiveSettingsForEncoding(Guid userId)
    {
        var hasSettings = await repository.UserHasSettings(userId);

        if (!hasSettings) await CreateDefaultSettings(userId);

        var activeSettings = await repository.GetActiveSettingsByUserId(userId);

        return activeSettings
            .OrderBy(s => (int)s.Resolution)
            .Select(s => new EncodingQualitySettingDto
            {
                Id = s.Id,
                Resolution = s.Resolution,
                BitrateKbps = s.BitrateKbps,
                IsEnabled = s.IsEnabled
            })
            .ToList();
    }

    private async Task CreateDefaultSettings(Guid userId)
    {
        var defaultResolutions = new[]
        {
            VideoEnum.VideoResolution._360p,
            VideoEnum.VideoResolution._480p,
            VideoEnum.VideoResolution._720p
        };

        foreach (var resolution in defaultResolutions)
            if (DefaultBitrates.TryGetValue(resolution, out var bitrate))
            {
                var setting = new EncodingQualitySetting
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Resolution = resolution,
                    BitrateKbps = bitrate,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };
                await repository.AddSetting(setting);
            }
    }
}