using MediatR;
using Modules.Chat.Application.Contracts;

namespace Modules.Chat.Application.Commands.Message
{
    public sealed record AddMessageToConversationCommand(Guid ConversationId, Guid SenderId, string Content) 
        : IRequest<Guid>;

    public class AddMessageToConversationCommandHandler(IMessageRepository messageRepo, IConversationRepository conversationRepo) : IRequestHandler<AddMessageToConversationCommand, Guid>
    {
        private readonly IMessageRepository _messageRepo = messageRepo;
        private readonly IConversationRepository _conversationRepo = conversationRepo;

        public async Task<Guid> Handle(AddMessageToConversationCommand req, CancellationToken ct)
        {
            var convo = await _conversationRepo.GetConversationByIdAsync(req.ConversationId, ct)
                ?? throw new InvalidOperationException("Conversation not found.");

            if (req.SenderId != convo.CustomerId && req.SenderId != convo.SellerId)
                throw new UnauthorizedAccessException("Sender is not in this conversation.");

            var message = new Domain.Message(req.ConversationId, req.SenderId, req.Content);

            await _messageRepo.AddAsync(message, ct);

            convo.TouchLastMessage(message);

            await _conversationRepo.SaveChangesAsync(ct); 

            return message.Id;
        }
    }
}

