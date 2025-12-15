using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContexts.Configurations;

internal class OnnxModelConfiguration : IEntityTypeConfiguration<OnnxModel>
{
    public void Configure(EntityTypeBuilder<OnnxModel> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(100).IsUnicode();
        builder.Property(x => x.FileLocation).HasMaxLength(200).IsUnicode();
        builder.Property(x => x.DemoInput)
            .HasMaxLength(200)
            .IsUnicode()
            .UsePropertyAccessMode(PropertyAccessMode.Property);
        builder.Property(x => x.DemoOutput)
            .HasMaxLength(200)
            .IsUnicode()
            .UsePropertyAccessMode(PropertyAccessMode.Property);
        builder.Property(x => x.Scale).HasDefaultValue(1);
    }
}