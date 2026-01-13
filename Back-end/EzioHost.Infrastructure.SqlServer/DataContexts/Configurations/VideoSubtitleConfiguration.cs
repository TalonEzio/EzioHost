using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContexts.Configurations;

internal class VideoSubtitleConfiguration : IEntityTypeConfiguration<VideoSubtitle>
{
    public void Configure(EntityTypeBuilder<VideoSubtitle> builder)
    {
        builder.Property(x => x.LocalPath)
            .HasMaxLength(500)
            .IsUnicode()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(x => x.CloudPath)
            .HasMaxLength(1000)
            .IsUnicode();

        builder.Property(x => x.Language)
            .HasMaxLength(50)
            .IsUnicode()
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsUnicode()
            .IsRequired();

        builder.HasOne(x => x.Video)
            .WithMany(x => x.VideoSubtitles)
            .HasForeignKey(x => x.VideoId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}