using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Auth.Domain;

namespace Modules.Auth.Infrastructure.Persistence
{
    public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
    {
        public DbSet<EmailVerificationOtp> EmailVerificationOtps => Set<EmailVerificationOtp>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EmailVerificationOtp>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.OtpCode).IsRequired().HasMaxLength(6);
                entity.Property(e => e.IsVerified).HasDefaultValue(false);
                entity.Property(e => e.AttemptCount).HasDefaultValue(0);

                entity.HasIndex(e => e.Email).IsUnique(false);
            });
        }
    }
}