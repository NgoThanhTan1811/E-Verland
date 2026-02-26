using Microsoft.EntityFrameworkCore;
using Modules.Auth.Domain;

namespace Modules.Auth.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Initial migration for EmailVerificationOtp table
    /// Run: dotnet ef database update
    /// </summary>
    public static class AuthDbInitialMigration
    {
        public static void CreateEmailVerificationOtpsTable(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EmailVerificationOtp>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.ToTable("email_verification_otps", "auth");

                entity.Property(e => e.Id)
                    .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("email");

                entity.Property(e => e.OtpCode)
                    .IsRequired()
                    .HasMaxLength(6)
                    .HasColumnName("otp_code");

                entity.Property(e => e.IsVerified)
                    .HasDefaultValue(false)
                    .HasColumnName("is_verified");

                entity.Property(e => e.ExpiresAt)
                    .HasColumnName("expires_at");

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(e => e.VerifiedAt)
                    .HasColumnName("verified_at");

                entity.Property(e => e.AttemptCount)
                    .HasDefaultValue(0)
                    .HasColumnName("attempt_count");

                entity.HasIndex(e => e.Email)
                    .HasDatabaseName("idx_email_verification_otps_email");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("idx_email_verification_otps_created_at");
            });
        }
    }
}
