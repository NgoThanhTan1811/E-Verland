using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Modules.Chat.Application.Contracts;
using Modules.Chat.Application.DTOs.Response;

namespace Modules.Chat.Application.Queries.Message
{
    public sealed record GetMessageByIdQuery(Guid MessageId) : IRequest<MessageResponseDto>;

    public class GetMessageByIdQueryHandler(IMessageRepository messageRepo) : IRequestHandler<GetMessageByIdQuery, MessageResponseDto>
    {
        private readonly IMessageRepository _messageRepo = messageRepo;

        public async Task<MessageResponseDto> Handle(GetMessageByIdQuery req, CancellationToken ct)
        {
            var message = await _messageRepo.GetByIdAsync(req.MessageId, ct)
                ?? throw new InvalidOperationException("Message not found.");

            return new MessageResponseDto(
                message.Id,
                message.ConversationId,
                message.SenderId,
                message.Content,
                message.SentAtUtc
            );
        }
    }


}