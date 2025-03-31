using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.Infrastructure.SqlServer.DataContext
{
    public class EzioHostDbContext(DbContextOptions<EzioHostDbContext> options) : DbContext(options)
    {
        public DbSet<Video> Videos { get; set; }
        public DbSet<FileUpload> FileUploads { get; set; }
        public DbSet<VideoStream> VideoStreams { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EzioHostDbContext).Assembly);
            
            ApplyGlobalFilters(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void ApplyGlobalFilters(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Video>().HasQueryFilter(x => !x.DeletedAt.HasValue);
            modelBuilder.Entity<UserSubscription>().HasQueryFilter(x => x.IsActive);
        }

        public override int SaveChanges()
        {
            HandleAuditableEntities();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            HandleAuditableEntities();
            return base.SaveChangesAsync(cancellationToken);
        }

        public void HandleAuditableEntities()
        {
            var entries = ChangeTracker.Entries();

            foreach (var entry in entries)
            {
                if (entry is { Entity: BaseCreatedEntity createdEntity, State: EntityState.Added })
                {
                    createdEntity.CreatedAt = DateTime.UtcNow;
                }

                if (entry.Entity is BaseAuditableEntity auditedEntity)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditedEntity.CreatedAt = DateTime.UtcNow;
                            auditedEntity.ModifiedAt = DateTime.UtcNow;
                            break;

                        case EntityState.Modified:
                            auditedEntity.ModifiedAt = DateTime.UtcNow;
                            entry.Property(nameof(auditedEntity.CreatedAt)).IsModified = false;
                            break;

                        case EntityState.Unchanged:
                            entry.State = EntityState.Modified;
                            auditedEntity.ModifiedAt = DateTime.UtcNow;
                            entry.Property(nameof(auditedEntity.CreatedAt)).IsModified = false;
                            break;

                        case EntityState.Deleted:
                            entry.State = EntityState.Modified;
                            auditedEntity.DeletedAt = DateTime.UtcNow;
                            break;
                    }
                }
            }
        }

        public async Task SeedData()
        {
            if (await SubscriptionPlans.AnyAsync())
            {
                await SubscriptionPlans.AddRangeAsync([
                    new SubscriptionPlan()
                    {
                        Name = "Monthly",
                        Description = "Monthly VIP",
                        DurationInDays = 30,
                        Id = Guid.NewGuid(),
                        Price = 7.99
                    },
                    new SubscriptionPlan()
                    {
                        Name = "Half Yearly",
                        Description = "Half Yearly VIP",
                        DurationInDays = 180,
                        Id = Guid.NewGuid(),
                        Price = 37.99
                    },
                    new SubscriptionPlan()
                    {
                        Name = "Yearly",
                        Description = "Yearly VIP",
                        DurationInDays = 30,
                        Id = Guid.NewGuid(),
                        Price = 77.99
                    }
                ]);
            }
        }

    }
}
