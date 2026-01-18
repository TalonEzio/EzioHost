using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContexts.Configurations;

internal class CloudflareStorageSettingConfiguration : IEntityTypeConfiguration<CloudflareStorageSetting>
{
    public void Configure(EntityTypeBuilder<CloudflareStorageSetting> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL");
    }
}
