using Microsoft.EntityFrameworkCore;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Domain;

namespace Modules.Payment.Infrastructure.Persistence
{
    public class PaymentDbContext(DbContextOptions<PaymentDbContext> options)
                : DbContext(options), IPaymentDbContext
    {
        public DbSet<Domain.Payment> Payments => Set<Domain.Payment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
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

                entity.HasIndex(x => x.UserId);
            });
        }
    }
}