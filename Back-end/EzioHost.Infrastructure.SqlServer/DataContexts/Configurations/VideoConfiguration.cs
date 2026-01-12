using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContexts.Configurations;

internal class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.Property(x => x.M3U8Location)
            .HasMaxLength(500)
            .IsUnicode()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(x => x.RawLocation)
            .HasMaxLength(500)
            .IsUnicode()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(x => x.Thumbnail)
            .HasMaxLength(500)
            .IsUnicode()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(x => x.Title).HasMaxLength(100).IsUnicode();

        builder.Property(x => x.BackupUrl)
            .HasMaxLength(1000)
            .IsUnicode()
            .IsRequired(false);

        builder.Property(x => x.BackupStatus)
            .HasConversion<byte>();
    }
}