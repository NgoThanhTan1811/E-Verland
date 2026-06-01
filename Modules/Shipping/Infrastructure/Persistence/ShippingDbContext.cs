using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Domain;
using SharedKernel.Entities;

namespace Modules.Shipping.Infrastructure.Persistence;

public sealed class ShippingDbContext(DbContextOptions<ShippingDbContext> options)
    : DbContext(options), IShippingDbContext
{
    public DbSet<ShippingOrder> ShippingOrders => Set<ShippingOrder>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct)
    {
        return Database.BeginTransactionAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);
                entityType.SetQueryFilter(filter);
                modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(BaseEntity.RowVersion))
                .IsConcurrencyToken()
                .ValueGeneratedNever();
            }
        }

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        modelBuilder.Entity<ShippingOrder>(b =>
        {
            b.ToTable("shipping_orders");

            b.HasKey(x => x.Id);

            b.HasIndex(x => x.OrderId).IsUnique();
            b.HasIndex(x => x.ProviderOrderCode);

            b.Property(x => x.Provider)
                .HasMaxLength(32)
                .IsRequired();

            b.Property(x => x.ProviderOrderCode)
                .HasMaxLength(64);

            b.Property(x => x.Status)
                .HasConversion<string>();

            b.Property(x => x.ProviderStatus)
                .HasMaxLength(64);

            b.Property(x => x.ToAddress)
                .HasConversion(new ValueConverter<ShippingAddressSnapshot, string>(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<ShippingAddressSnapshot>(v, jsonOptions)!))
                .HasColumnType("jsonb");

            b.Property(x => x.FromAddress)
                .HasConversion(new ValueConverter<ShippingAddressSnapshot?, string?>(
                    v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<ShippingAddressSnapshot>(v, jsonOptions)))
                .HasColumnType("jsonb");

            b.Property(x => x.Items)
                .HasConversion(new ValueConverter<List<ShippingItemSnapshot>, string>(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<List<ShippingItemSnapshot>>(v, jsonOptions) ?? new List<ShippingItemSnapshot>()))
                .HasColumnType("jsonb");

            b.Property(x => x.FeeSnapshot)
                .HasConversion(new ValueConverter<ShippingFeeSnapshot?, string?>(
                    v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
                    v => v == null ? null : JsonSerializer.Deserialize<ShippingFeeSnapshot>(v, jsonOptions)))
                .HasColumnType("jsonb");
        });
    }
}
