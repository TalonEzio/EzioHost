using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContexts.Configurations
{
    internal class VideoUpscaleConfiguration : IEntityTypeConfiguration<VideoUpscale>
    {
        public void Configure(EntityTypeBuilder<VideoUpscale> builder)
        {
            builder.Property(x => x.Scale).HasDefaultValue(1);
            builder.Property(x => x.OutputLocation).HasMaxLength(200).IsUnicode();
        }
    }
}
