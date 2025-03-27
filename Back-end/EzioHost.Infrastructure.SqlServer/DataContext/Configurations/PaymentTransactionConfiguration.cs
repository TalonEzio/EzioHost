using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EzioHost.Infrastructure.SqlServer.DataContext.Configurations
{
    internal class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {

            builder.Property(x=>x.PaymentProvider).HasMaxLength(50).IsUnicode();
        }
    }
}
