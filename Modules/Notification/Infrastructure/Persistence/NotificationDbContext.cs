using Microsoft.EntityFrameworkCore;

namespace Modules.Notification.Infrastructure.Persistence;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options)
    : DbContext(options)
{
    public DbSet<Domain.Notification> Notifications => Set<Domain.Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
