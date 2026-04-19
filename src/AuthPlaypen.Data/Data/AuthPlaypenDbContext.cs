using AuthPlaypen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthPlaypen.Data.Data;

public class AuthPlaypenDbContext(DbContextOptions<AuthPlaypenDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<ScopeEntity> Scopes => Set<ScopeEntity>();
    public DbSet<ApplicationScopeEntity> ApplicationScopes => Set<ApplicationScopeEntity>();
    public DbSet<EntityAuditHistoryEntry> EntityAuditHistory => Set<EntityAuditHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationEntity>(entity =>
        {
            entity.ToTable("applications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).IsRequired();
            entity.Property(x => x.ClientId).IsRequired();
            entity.Property(x => x.ClientSecret).IsRequired();
            entity.Property(x => x.Flow).HasConversion<string>().IsRequired();
            entity.Property(x => x.CreatedBy).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedBy).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => x.ClientId).IsUnique();
        });

        modelBuilder.Entity<ScopeEntity>(entity =>
        {
            entity.ToTable("scopes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).IsRequired();
            entity.Property(x => x.ScopeName).IsRequired();
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.CreatedBy).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedBy).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => x.ScopeName).IsUnique();
        });

        modelBuilder.Entity<ApplicationScopeEntity>(entity =>
        {
            entity.ToTable("application_scopes");
            entity.HasKey(x => new { x.ApplicationId, x.ScopeId });

            entity.HasOne(x => x.Application)
                .WithMany(x => x.ApplicationScopes)
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Scope)
                .WithMany(x => x.ApplicationScopes)
                .HasForeignKey(x => x.ScopeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EntityAuditHistoryEntry>(entity =>
        {
            entity.ToTable("entity_audit_history");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EntityType).IsRequired();
            entity.Property(x => x.EntityId).IsRequired();
            entity.Property(x => x.Action).IsRequired();
            entity.Property(x => x.ActorDisplayName).IsRequired();
            entity.Property(x => x.ActorEmail);
            entity.Property(x => x.OccurredAt).IsRequired();
            entity.Property(x => x.ChangeSummaryJson);
            entity.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt });
        });
    }
}
