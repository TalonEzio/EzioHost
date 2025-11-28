using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContexts.Configurations
{
    public class FileUploadConfiguration : IEntityTypeConfiguration<FileUpload>
    {
        public void Configure(EntityTypeBuilder<FileUpload> builder)
        {
            builder.Property(x => x.FileName).HasMaxLength(200).IsUnicode();
            builder.Property(x => x.ContentType).HasMaxLength(100).IsUnicode();
            builder.Property(x => x.Checksum).HasMaxLength(512).IsUnicode();

        }
    }
}
