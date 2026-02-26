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
    public class MessageRepository(ChatDbContext dbContext) : IMessageRepository
    {
        private readonly ChatDbContext _dbContext = dbContext;
        public async Task<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Message>> AddAsync(Message message, CancellationToken ct = default)
        {
            return await _dbContext.Messages.AddAsync(message, ct);
        }

        public async Task<Message?> GetByIdAsync(Guid messageId, CancellationToken ct = default)
        {
            return await _dbContext.Messages.FindAsync([messageId], cancellationToken: ct);
        }

        public async Task<List<Message>> GetMessagesAsync(Guid conversationId, DateTime? beforeUtc = null, int take = 30, CancellationToken ct = default)
        {
            take = Math.Clamp(take, 1, 50);

            var q = _dbContext.Messages
                .AsNoTracking()
                .Where(x => x.ConversationId == conversationId);

            if (beforeUtc.HasValue)
                q = q.Where(x => x.SentAtUtc < beforeUtc.Value);

            return await q.OrderByDescending(x => x.SentAtUtc)
                    .ThenByDescending(x => x.Id)
                    .Take(take)
                    .ToListAsync(ct);
        }
    }
}
