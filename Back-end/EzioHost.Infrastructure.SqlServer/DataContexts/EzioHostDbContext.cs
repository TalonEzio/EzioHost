using EzioHost.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.Infrastructure.SqlServer.DataContexts;

public class EzioHostDbContext(DbContextOptions<EzioHostDbContext> options) : DbContext(options)
{
    public DbSet<Video> Videos { get; set; }
    public DbSet<FileUpload> FileUploads { get; set; }
    public DbSet<VideoStream> VideoStreams { get; set; }
    public DbSet<User> Users { get; set; }

    public DbSet<OnnxModel> OnnxModels { get; set; }

    public DbSet<VideoUpscale> VideoUpscales { get; set; }

    public DbSet<VideoSubtitle> VideoSubtitles { get; set; }

    public DbSet<EncodingQualitySetting> EncodingQualitySettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EzioHostDbContext).Assembly);

        ApplyGlobalFilters(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void ApplyGlobalFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Video>().HasQueryFilter(x => !x.DeletedAt.HasValue);
        modelBuilder.Entity<OnnxModel>().HasQueryFilter(x => !x.DeletedAt.HasValue);
        modelBuilder.Entity<EncodingQualitySetting>().HasQueryFilter(x => !x.DeletedAt.HasValue);
        modelBuilder.Entity<VideoSubtitle>().HasQueryFilter(x => !x.DeletedAt.HasValue);

        modelBuilder.Entity<VideoUpscale>().HasQueryFilter(x =>
            !x.DeletedAt.HasValue && !x.Model.DeletedAt.HasValue && !x.Video.DeletedAt.HasValue);
    }

    public override int SaveChanges()
    {
        HandleAuditableEntities();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
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
                createdEntity.CreatedAt = DateTime.UtcNow;

            if (entry.Entity is BaseAuditableEntity auditedEntity)
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