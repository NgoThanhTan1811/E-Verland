using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Modules.Product.Application.Contracts;
using Modules.Product.Domain;

namespace Modules.Product.Infrastructure.Persistence;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options), IProductDbContext
{
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Domain.Product> Products => Set<Domain.Product>();
    public DbSet<SKU> SKUs => Set<SKU>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var imageUrlsConverter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v ?? new List<string>(), jsonOptions),
            v => string.IsNullOrWhiteSpace(v)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>()
        );

        var attributesConverter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v ?? new(), jsonOptions),
            v => string.IsNullOrWhiteSpace(v)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new()
        );

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("Brands");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasOne(x => x.ParentCategory)
                .WithMany(x => x.SubCategories)
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        _ = modelBuilder.Entity<Domain.Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(x => x.VirtualPrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.BasePrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .IsRequired();

            entity.Property(x => x.ImageUrls)
                .HasConversion(imageUrlsConverter)
                .HasColumnType("jsonb");

            entity.Property(x => x.Attributes)
                .HasConversion(attributesConverter)
                .HasColumnType("jsonb");

            entity.HasOne(x => x.Brand)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.Attributes).HasMethod("GIN");
            entity.HasIndex(x => x.Slug);

            entity.HasMany(x => x.Categories)
                .WithMany(x => x.Products)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductCategories",
                    j => j.HasOne<Category>()
                        .WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Domain.Product>()
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                );
        });

        modelBuilder.Entity<SKU>(entity =>
        {
            entity.ToTable("Skus");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.SkuCode)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Url)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(x => x.Price)
                .HasPrecision(18, 2);

            entity.Property(x => x.OptionValues)
                .HasConversion(attributesConverter)
                .HasColumnType("jsonb");

            entity.HasIndex(x => x.OptionValues).HasMethod("GIN");

            entity.HasIndex(x => x.SkuCode).IsUnique();

            entity.HasOne(x => x.Product)
                .WithMany(x => x.SKUs)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
