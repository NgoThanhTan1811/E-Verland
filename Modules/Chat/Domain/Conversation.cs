using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.Chat.Domain
{
    public sealed class Conversation
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid CustomerId { get; private set; }
        public Guid SellerId { get; private set; }

        public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

        public Guid? LastMessageId { get; private set; }
        public string? LastMessagePreview { get; private set; }
        public DateTime? LastMessageAtUtc { get; private set; }

        private Conversation() { }

        public Conversation(Guid customerId, Guid sellerId)
        {
            if (customerId == Guid.Empty) throw new ArgumentException("CustomerId required");
            if (sellerId == Guid.Empty) throw new ArgumentException("SellerId required");

            CustomerId = customerId;
            SellerId = sellerId;
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