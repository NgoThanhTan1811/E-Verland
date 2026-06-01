using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        public DbSet<LedgerTransaction> LedgerTransactions => Set<LedgerTransaction>();
        public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
        public DbSet<BalanceSnapshot> BalanceSnapshots => Set<BalanceSnapshot>();
        public DbSet<SellerBalance> SellerBalances => Set<SellerBalance>();
        public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();

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

            modelBuilder.Entity<WebhookEvent>(entity =>
            {
                entity.ToTable("WebhookEvents");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.TransactionId)
                    .HasMaxLength(128)
                    .IsRequired();

                entity.Property(x => x.PaymentCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.EventStatus)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasIndex(x => x.TransactionId)
                    .IsUnique();
            });

            modelBuilder.Entity<LedgerTransaction>(entity =>
            {
                entity.ToTable("LedgerTransactions");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.IdempotencyKey)
                    .HasMaxLength(128)
                    .IsRequired();

                entity.Property(x => x.Currency)
                    .HasMaxLength(3)
                    .IsRequired();

                entity.Property(x => x.CreatedBy)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .IsRequired();

                entity.HasIndex(x => x.IdempotencyKey)
                    .IsUnique();
                entity.HasIndex(x => x.OrderId);
                entity.HasIndex(x => x.PayoutId);
                entity.HasIndex(x => x.TimestampUtc);
            });

            modelBuilder.Entity<LedgerEntry>(entity =>
            {
                entity.ToTable("LedgerEntries");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.EntryType)
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(x => x.AccountType)
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(x => x.Currency)
                    .HasMaxLength(3)
                    .IsRequired();

                entity.Property(x => x.CreatedBy)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasOne(x => x.LedgerTransaction)
                    .WithMany(x => x.Entries)
                    .HasForeignKey(x => x.LedgerTransactionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.AccountType);
                entity.HasIndex(x => x.TimestampUtc);
            });

            modelBuilder.Entity<BalanceSnapshot>(entity =>
            {
                entity.ToTable("BalanceSnapshots");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.AccountType)
                    .HasConversion<string>()
                    .IsRequired();

                entity.Property(x => x.Balance)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.HasOne(x => x.LedgerTransaction)
                    .WithMany()
                    .HasForeignKey(x => x.LedgerTransactionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.AccountType);
                entity.HasIndex(x => x.SnapshotAtUtc);
            });

            modelBuilder.Entity<SellerBalance>(entity =>
            {
                entity.ToTable("SellerBalances");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.PendingAmount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(x => x.AvailableAmount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(x => x.Currency)
                    .HasMaxLength(3)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .IsRequired();

                entity.HasIndex(x => x.OrderId);
                entity.HasIndex(x => x.SellerId);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.AvailableAtUtc);
            });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            GuardLedgerImmutability();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void GuardLedgerImmutability()
        {
            static bool IsLedgerImmutable(EntityEntry entry)
            {
                var entityType = entry.Entity.GetType();
                return entityType == typeof(LedgerTransaction) || entityType == typeof(LedgerEntry);
            }

            foreach (var entry in ChangeTracker.Entries().Where(e =>
                         (e.State == EntityState.Modified || e.State == EntityState.Deleted) &&
                         IsLedgerImmutable(e)))
            {
                throw new InvalidOperationException("Ledger entries are immutable and cannot be updated or deleted.");
            }
        }
    }
}
