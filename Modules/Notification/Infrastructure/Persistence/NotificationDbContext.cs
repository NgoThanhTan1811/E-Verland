using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Entities;

namespace Modules.Notification.Infrastructure.Persistence;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options)
    : DbContext(options)
{
    public DbSet<Domain.Notification> Notifications => Set<Domain.Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global soft delete filter
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);
                entityType.SetQueryFilter(filter);
                modelBuilder.Entity(entityType.ClrType).Property(nameof(BaseEntity.RowVersion)).IsRowVersion();
            }
        }

        modelBuilder.Entity<Domain.Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.AdminId).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.IsRead).IsRequired();

            // Indexes for common queries
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_notification_userid");
            entity.HasIndex(e => new { e.UserId, e.IsRead }).HasDatabaseName("idx_notification_userid_isread");
            entity.HasIndex(e => e.CreatedAtUtc).HasDatabaseName("idx_notification_createdat");
        });
    }
}
