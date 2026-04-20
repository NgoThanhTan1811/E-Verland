using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Modules.Order.Application.Contracts;
using Modules.Order.Domain;
using SharedKernel.Entities;

namespace Modules.Order.Infrastructure.Persistence;

public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options), IOrderDbContext
{
    public DbSet<Domain.Order> Orders => Set<Domain.Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

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

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        modelBuilder.Entity<Domain.Order>(b =>
        {
            b.ToTable("orders");

            b.HasKey(x => x.Id);

            b.Property(x => x.Code).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();

            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.PaymentStatus).HasConversion<string>();
            b.Property(x => x.PaymentMethod).HasConversion<string>();

            // jsonb for ReceiverSnapshot
            b.Property(x => x.Receiver)
             .HasConversion(new ValueConverter<ReceiverSnapshot, string>(
                 v => JsonSerializer.Serialize(v, jsonOptions),
                 v => JsonSerializer.Deserialize<ReceiverSnapshot>(v, jsonOptions)!
             ))
             .HasColumnType("jsonb")
             .IsRequired();

            // Items
            b.HasMany(x => x.Items)
             .WithOne(x => x.Order)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(b =>
        {
            b.ToTable("order_items");
            b.HasKey(x => x.Id);

            b.Property(x => x.ProductName).IsRequired();
            b.Property(x => x.UnitPrice).HasColumnType("numeric(18,2)");
        });
    }
}