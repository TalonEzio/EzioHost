using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Models;

namespace EzioHost.Core.Services.Implement;

public class SubtitleTranscribeSettingService(
    ISubtitleTranscribeSettingRepository repository) : ISubtitleTranscribeSettingService
{
    public async Task<SubtitleTranscribeSettingDto> GetUserSettingsAsync(Guid userId)
    {
        var setting = await repository.GetByUserIdAsync(userId);

        if (setting == null)
        {
            // Create default settings
            setting = new SubtitleTranscribeSetting
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                IsEnabled = true,
                ModelType = WhisperEnum.WhisperModelType.Base,
                UseGpu = false,
                GpuDeviceId = null,
                CreatedBy = userId,
                ModifiedBy = userId
            };

            setting = await repository.CreateOrUpdateAsync(setting);
        }

        return new SubtitleTranscribeSettingDto
        {
            Id = setting.Id,
            IsEnabled = setting.IsEnabled,
            ModelType = setting.ModelType,
            UseGpu = setting.UseGpu,
            GpuDeviceId = setting.GpuDeviceId
        };
    }

    public async Task<SubtitleTranscribeSettingDto> UpdateUserSettingsAsync(Guid userId,
        SubtitleTranscribeSettingUpdateDto dto)
    {
        var setting = await repository.GetByUserIdAsync(userId);

        if (setting == null)
        {
            setting = new SubtitleTranscribeSetting
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedBy = userId,
                ModifiedBy = userId
            };
        }

        setting.IsEnabled = dto.IsEnabled;
        setting.ModelType = dto.ModelType;
        setting.UseGpu = dto.UseGpu;
        setting.GpuDeviceId = dto.GpuDeviceId;
        setting.ModifiedBy = userId;

        setting = await repository.CreateOrUpdateAsync(setting);

        return new SubtitleTranscribeSettingDto
        {
            Id = setting.Id,
            IsEnabled = setting.IsEnabled,
            ModelType = setting.ModelType,
            UseGpu = setting.UseGpu,
            GpuDeviceId = setting.GpuDeviceId
        };
    }
}
