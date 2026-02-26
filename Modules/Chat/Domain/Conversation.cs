using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.Chat.Domain
{
    public sealed class Conversation
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid UserId { get; private set; }       
        public Guid AdminId { get; private set; }      

        public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

        public Guid? LastMessageId { get; private set; }
        public string? LastMessagePreview { get; private set; }
        public DateTime? LastMessageAtUtc { get; private set; }

        private Conversation() { }

        public Conversation(Guid userId, Guid adminId)
        {
            if (userId == Guid.Empty) throw new ArgumentException("UserId required");
            if (adminId == Guid.Empty) throw new ArgumentException("AdminId required");

            UserId = userId;
            AdminId = adminId;
        }

        public void TouchLastMessage(Message message)
        {
            LastMessageId = message.Id;
            LastMessageAtUtc = message.SentAtUtc;
            LastMessagePreview =
                message.Content.Length <= 100
                    ? message.Content
                    : message.Content[..100];
        }
    }
}