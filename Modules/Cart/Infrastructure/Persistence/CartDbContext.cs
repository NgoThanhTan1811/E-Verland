using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Modules.Cart.Application.Contracts;
using Modules.Cart.Domain;

namespace Modules.Cart.Infrastructure.Persistence;

public class CartDbContext(DbContextOptions<CartDbContext> options) : DbContext(options), ICartDbContext
{
    public DbSet<Domain.Cart> Carts => Set<Domain.Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<Domain.Cart>(entity =>
        {
            entity.ToTable("Carts");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId)
                .IsRequired();

            entity.HasMany(x => x.Items)
                .WithOne(x => x.Cart)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.UserId)
                .IsUnique();
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ProductId)
                .IsRequired();

            entity.Property(x => x.Quantity)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.CartId,
                x.ProductId,
                x.SkuId
            })
                .IsUnique();


        });
    }



}