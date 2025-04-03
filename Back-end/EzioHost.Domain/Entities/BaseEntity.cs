namespace EzioHost.Domain.Entities
{
    public class BaseCreatedEntity
    {
        public DateTime CreatedAt { get; set; }
    }

    public class BaseAuditableEntity : BaseCreatedEntity
    {
        public DateTime ModifiedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public bool IsDeleted => DeletedAt.HasValue;
    }

    public class BaseCreatedEntityWithUserId<T> : BaseCreatedEntity where T : struct
    {
        public T CreatedBy { get; set; }
    }

    public class BaseAuditableEntityWithUserId<T> : BaseAuditableEntity where T : struct
    {
        public T CreatedBy { get; set; }
        public T ModifiedBy { get; set; }
    }
}