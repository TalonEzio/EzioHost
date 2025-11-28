using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContexts.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(x => x.FirstName).HasMaxLength(50).IsUnicode();
            builder.Property(x => x.LastName).HasMaxLength(50).IsUnicode();
            builder.Property(x => x.UserName).HasMaxLength(32).IsUnicode();
            builder.Property(x => x.Email).HasMaxLength(50).IsUnicode();
        }
    }
}
