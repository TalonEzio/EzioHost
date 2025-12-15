using System.ComponentModel.DataAnnotations.Schema;

namespace EzioHost.Domain.Entities;

[Table("Users")]
public class User : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;
}