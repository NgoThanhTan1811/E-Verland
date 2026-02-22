using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Modules.Order.Application.Contracts;
using Modules.Order.Domain;

namespace Modules.Order.Infrastructure.Persistence;

public class OrderDbContext : DbContext, IOrderDbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Domain.Order> Orders => Set<Domain.Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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