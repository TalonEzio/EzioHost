using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContexts.Configurations;

internal class SubtitleTranscribeConfiguration : IEntityTypeConfiguration<SubtitleTranscribe>
{
    public void Configure(EntityTypeBuilder<SubtitleTranscribe> builder)
    {
        builder.Property(x => x.Language)
            .HasMaxLength(50)
            .IsUnicode()
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000)
            .IsUnicode();

        builder.HasOne(x => x.Video)
            .WithMany()
            .HasForeignKey(x => x.VideoId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
