using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContext.Configurations
{
    internal class VideoStreamConfiguration : IEntityTypeConfiguration<VideoStream>
    {
        public void Configure(EntityTypeBuilder<VideoStream> builder)
        {
            builder.Property(x=>x.M3U8Location).HasMaxLength(500).IsUnicode();
        }
    }
}
