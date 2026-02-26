using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Chat.Application.Contracts;
using Modules.Chat.Domain;
using Modules.Chat.Infrastructure.Persistence;

namespace Modules.Chat.Infrastructure.Repository
{
    public class ConversationRepository(ChatDbContext dbContext) : IConversationRepository
    {
        private readonly ChatDbContext _dbContext = dbContext;
        public async Task<Conversation?> GetConversationByIdAsync(Guid conversationId, CancellationToken ct = default)
        {
            return await _dbContext.Conversations.FindAsync([conversationId], cancellationToken: ct);
        }

        public async Task<Conversation?> GetConversationByUserAsync(Guid userId, Guid adminId, CancellationToken ct = default)
        {

            return await _dbContext.Conversations.FirstOrDefaultAsync(c => c.UserId == userId && c.AdminId == adminId, cancellationToken: ct);
        }

        public async Task<List<Conversation>> GetConversationsForAdminAsync(Guid adminId, CancellationToken ct = default)
        {
            return await _dbContext.Conversations.Where(c => c.AdminId == adminId).ToListAsync(cancellationToken: ct);
        }

        public async Task<Conversation> GetOrCreateConversationAsync(Guid userId, Guid adminId, CancellationToken ct = default)
        {
            var existing = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.UserId == userId && c.AdminId == adminId, ct);

            if (existing != null) return existing;

            var conversation = new Conversation(userId, adminId);

            _dbContext.Conversations.Add(conversation);

            try
            {
                await _dbContext.SaveChangesAsync(ct);
                return conversation;
            }
            catch (DbUpdateException)
            {
                var created = await _dbContext.Conversations
                    .FirstAsync(c => c.UserId == userId && c.AdminId == adminId, ct);
                return created;
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) 
            => _dbContext.SaveChangesAsync(ct);
    }
}