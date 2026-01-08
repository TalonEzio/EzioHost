using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContexts.Configurations;

internal class VideoStreamConfiguration : IEntityTypeConfiguration<VideoStream>
{
    public void Configure(EntityTypeBuilder<VideoStream> builder)
    {
        builder.Property(x => x.M3U8Location)
            .HasMaxLength(500)
            .IsUnicode()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(x => x.Key).IsRequired().HasMaxLength(32).IsUnicode(false);
        builder.Property(x => x.Iv).IsRequired().HasMaxLength(32).IsUnicode(false);

        // Make Video navigation optional to avoid issues with global query filter
        builder.HasOne(x => x.Video)
            .WithMany(x => x.VideoStreams)
            .HasForeignKey(x => x.VideoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}