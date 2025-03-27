using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EzioHost.Domain.Entities
{
    [Table("SubscriptionPlans")]

    public class SubscriptionPlan
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public double Price { get; set; }

        public int DurationInDays { get; set; }

        public string Description { get; set; } = string.Empty;

        public ICollection<UserSubscription> UserSubscriptions { get; set; } = [];
        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = [];
    }
}
