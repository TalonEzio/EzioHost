using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContexts.Configurations;

internal class SubtitleTranscribeSettingConfiguration : IEntityTypeConfiguration<SubtitleTranscribeSetting>
{
    public void Configure(EntityTypeBuilder<SubtitleTranscribeSetting> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ModelType)
            .HasConversion<byte>()
            .IsRequired()
            .HasDefaultValue(WhisperEnum.WhisperModelType.Base);

        builder.Property(x => x.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.UseGpu)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL");
    }
}
