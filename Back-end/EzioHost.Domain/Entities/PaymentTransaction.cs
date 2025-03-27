using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EzioHost.Domain.Entities
{
    [Table("PaymentTransactions")]
    public class PaymentTransaction
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid SubscriptionPlanId { get; set; }

        public SubscriptionPlan? SubscriptionPlan { get; set; }

        public decimal AmountPaid { get; set; }

        [Required]
        public string PaymentProvider { get; set; } = "PayPal";

        public string TransactionId { get; set; } = string.Empty;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
    }

}
