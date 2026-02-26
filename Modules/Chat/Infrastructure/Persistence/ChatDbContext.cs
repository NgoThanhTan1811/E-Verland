using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Modules.Chat.Infrastructure.Persistence
{
    public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
    {
        public DbSet<Domain.Conversation> Conversations => Set<Domain.Conversation>();
        public DbSet<Domain.Message> Messages => Set<Domain.Message>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Domain.Conversation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.AdminId).IsRequired();
                entity.Property(e => e.CreatedAtUtc).IsRequired();
                entity.HasIndex(e => new { e.UserId, e.AdminId }).IsUnique();
            });

            modelBuilder.Entity<Domain.Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ConversationId).IsRequired();
                entity.Property(e => e.SenderId).IsRequired();
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.SentAtUtc).IsRequired();
                entity.HasIndex(e => e.ConversationId);

                entity.HasOne<Domain.Conversation>()
                    .WithMany()
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

    }
}