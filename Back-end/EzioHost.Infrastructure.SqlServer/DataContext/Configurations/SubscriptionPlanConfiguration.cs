using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContext.Configurations
{
    internal class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
        {
            builder.Property(x => x.Description).HasMaxLength(500).IsUnicode();
            builder.Property(x => x.Name).HasMaxLength(50).IsUnicode();
        }
    }
}
