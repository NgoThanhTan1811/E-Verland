using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Entities;

namespace Modules.Media.Infrastructure.Data;

public class MediaDbContext : DbContext
{
    public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.MediaFile> MediaFiles { get; set; } = default!;

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
            }
        }

        modelBuilder.Entity<Domain.MediaFile>(entity =>
        {
            entity.ToTable("MediaFiles");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.FilePath)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.MediaType)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(e => e.UploadedBy)
                .IsRequired();

            entity.Property(e => e.UploadedAt)
                .IsRequired();

            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            // Indexes
            entity.HasIndex(e => e.UploadedBy);
            entity.HasIndex(e => e.MediaType);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => e.UploadedAt);
        });
    }
}
