
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Modules.User.Domain.Entities;
using Modules.User.Application.Interfaces.Repositories;
using SharedKernel.Entities;

namespace Modules.User.Infrastructure.Persistence
{
      public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options), IUserDbContext
      {
            public DbSet<Account> Accounts { get; set; }
            public DbSet<Profile> Profiles { get; set; }
            public DbSet<Address> Addresses { get; set; }
            public DbSet<BankAccount> BankAccounts { get; set; }



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

                  #region Account

                  modelBuilder.Entity<Account>(entity =>
                  {
                        entity.ToTable("Accounts");

                        entity.HasKey(x => x.Id);

                        entity.Property(x => x.Email)
                     .IsRequired()
                     .HasMaxLength(256);

                        entity.Property(x => x.Username)
                     .IsRequired()
                     .HasMaxLength(100);

                        entity.Property(x => x.NormalizedUsername)
                     .IsRequired()
                     .HasMaxLength(100);

                        entity.Property(x => x.NormalizedEmail)
                     .IsRequired()
                     .HasMaxLength(256);

                        entity.Property(x => x.Password)
                     .IsRequired()
                     .HasMaxLength(500);

                        entity.Property(x => x.Role)
                     .HasConversion<string>()
                     .IsRequired();

                        entity.Property(x => x.Status)
                     .HasConversion<string>()
                     .IsRequired();

                        entity.HasOne(x => x.Profile)
                     .WithOne(x => x.Account)
                     .HasForeignKey<Profile>(x => x.AccountId)
                     .OnDelete(DeleteBehavior.Cascade);

                        entity.Property<uint>("xmin")
                     .HasColumnName("xmin")
                     .IsConcurrencyToken()
                     .ValueGeneratedOnAddOrUpdate();
                  });
                  #endregion

                  #region Profile

                  modelBuilder.Entity<Profile>(entity =>
                     {
                           entity.ToTable("Profiles");

                           entity.HasKey(x => x.Id);

                           entity.Property(x => x.FirstName)
                        .HasMaxLength(100);

                           entity.Property(x => x.LastName)
                        .HasMaxLength(100);

                           entity.Property(x => x.PhoneNumber)
                        .HasMaxLength(30);

                           entity.Property(x => x.AvatarUrl)
                        .HasMaxLength(500);

                           entity.Property(x => x.Gender)
                        .HasConversion<string>()
                        .IsRequired();

                           entity.HasMany(x => x.Addresses)
                        .WithOne(x => x.Profile)
                        .HasForeignKey(x => x.ProfileId)
                        .OnDelete(DeleteBehavior.Cascade);

                           entity.HasMany(x => x.BankAccounts)
                        .WithOne(x => x.Profile)
                        .HasForeignKey(x => x.ProfileId)
                        .OnDelete(DeleteBehavior.Cascade);

                           entity.Property<uint>("xmin")
                        .HasColumnName("xmin")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                     });
                  #endregion

                  #region Address 
                  modelBuilder.Entity<Address>(entity =>
                     {
                           entity.ToTable("Addresses");

                           entity.HasKey(x => x.Id);

                           entity.Property(x => x.Label)
                              .HasMaxLength(50);

                           entity.Property(x => x.Province)
                              .IsRequired()
                              .HasMaxLength(100);

                           entity.Property(x => x.District)
                              .IsRequired()
                              .HasMaxLength(100);

                           entity.Property(x => x.Ward)
                              .IsRequired()
                              .HasMaxLength(100);

                           entity.Property(x => x.Detail)
                              .IsRequired()
                              .HasMaxLength(300);

                           entity.Property(x => x.City)
                             .IsRequired()
                             .HasMaxLength(100);
                           entity.Property(x => x.Street)
                              .IsRequired()
                              .HasMaxLength(100);

                           entity.Property(x => x.IsDefault)
                              .IsRequired();

                           entity.HasIndex(x => new { x.ProfileId, x.IsDefault })
                     .HasDatabaseName("IX_ProfileId_IsDefault");

                           entity.Property<uint>("xmin")
                        .HasColumnName("xmin")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                     });
                  #endregion

                  #region BankAccount
                  modelBuilder.Entity<BankAccount>(entity =>
                  {
                        entity.ToTable("BankAccounts");

                        entity.HasKey(x => x.Id);

                        entity.Property(x => x.BankName)
                              .IsRequired()
                              .HasMaxLength(200);

                        entity.Property(x => x.BankCode)
                              .IsRequired()
                              .HasMaxLength(30);

                        entity.Property(x => x.AccountNumber)
                              .IsRequired()
                              .HasMaxLength(50);

                        entity.Property(x => x.AccountHolder)
                              .IsRequired()
                              .HasMaxLength(200);

                        entity.HasIndex(x => x.ProfileId);

                        entity.HasIndex(x => new { x.ProfileId, x.BankCode, x.AccountNumber })
            .IsUnique();

                        entity.Property<uint>("xmin")
                     .HasColumnName("xmin")
                     .IsConcurrencyToken()
                     .ValueGeneratedOnAddOrUpdate();

                  });
                  #endregion

            }
      }
}

