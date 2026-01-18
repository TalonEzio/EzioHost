using System.ComponentModel.DataAnnotations.Schema;

namespace EzioHost.Domain.Entities;

[Table("CloudflareStorageSettings")]
public class CloudflareStorageSetting : BaseAuditableEntityWithUserId<Guid>
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public bool IsEnabled { get; set; } = true;
}
