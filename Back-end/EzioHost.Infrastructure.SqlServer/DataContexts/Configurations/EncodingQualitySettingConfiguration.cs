using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContexts.Configurations;

internal class EncodingQualitySettingConfiguration : IEntityTypeConfiguration<EncodingQualitySetting>
{
    public void Configure(EntityTypeBuilder<EncodingQualitySetting> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Resolution)
            .HasConversion<int>();

        builder.Property(x => x.BitrateKbps)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => new { x.UserId, x.Resolution })
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL");
    }
}