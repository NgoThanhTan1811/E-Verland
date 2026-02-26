using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Modules.Chat.Application.Contracts;
using Modules.Chat.Application.DTOs.Response;

namespace Modules.Chat.Application.Queries.Message
{
    public sealed record GetMessagesByConversationIdQuery(Guid ConversationId) : IRequest<List<MessageResponseDto>>;
    public class GetMessagesByConversationIdQueryHandler(IMessageRepository messageRepo) 
        : IRequestHandler<GetMessagesByConversationIdQuery, List<MessageResponseDto>>
    {
        private readonly IMessageRepository _messageRepo = messageRepo;

        public async Task<List<MessageResponseDto>> Handle(GetMessagesByConversationIdQuery req, CancellationToken ct)
        {
            var messages = await _messageRepo.GetMessagesAsync(req.ConversationId, ct: ct);

            return [.. messages.Select(message => new MessageResponseDto(
                message.Id,
                message.ConversationId,
                message.SenderId,
                message.Content,
                message.SentAtUtc
            ))];
        }
    }
}