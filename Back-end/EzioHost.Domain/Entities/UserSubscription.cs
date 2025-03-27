using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EzioHost.Domain.Entities
{
    [Table("UserSubscriptions")]

    public class UserSubscription
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }
        public User User { get; set; } = new();

        [Required]
        public Guid SubscriptionPlanId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; } = new();

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime ExpiryDate  => StartDate.AddDays(SubscriptionPlan.DurationInDays);

        [NotMapped]
        public bool IsActive => ExpiryDate > DateTime.UtcNow;
    }

}
