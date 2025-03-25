using System.Security.Claims;

namespace EzioHost.Shared.Models
{
    public class ClaimDto
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public static  Claim ConvertToClaim(ClaimDto instance)
        {
            return new Claim(instance.Type, instance.Value);
        }
        public static ClaimDto ConvertFromClaim(Claim claim)
        {
            return new ClaimDto
            {
                Type = claim.Type,
                Value = claim.Value
            };
        }
    }
}
