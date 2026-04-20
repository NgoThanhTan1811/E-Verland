using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Domain;
using SharedKernel.Entities;

namespace Modules.Payment.Infrastructure.Persistence
{
    public class PaymentDbContext(DbContextOptions<PaymentDbContext> options)
                : DbContext(options), IPaymentDbContext
    {
        public DbSet<Domain.Payment> Payments => Set<Domain.Payment>();

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

            modelBuilder.Entity<Domain.Payment>(entity =>
            {
                entity.ToTable("Payments");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Code)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.OrderId)
                    .IsRequired();

                entity.Property(x => x.UserId)
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(x => x.Method)
                    .HasConversion<string>()
                    .HasDefaultValue(PaymentMethod.COD)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasDefaultValue(PaymentStatus.Pending)
                    .IsRequired();

                entity.HasIndex(x => x.Code)
                    .IsUnique();

                entity.HasIndex(x => x.OrderId)
                    .IsUnique();

                entity.Property(x => x.PaymentUrl)
                    .HasColumnType("text")
                    .IsRequired(false);

                entity.HasIndex(x => x.UserId);
            });
        }
    }
}