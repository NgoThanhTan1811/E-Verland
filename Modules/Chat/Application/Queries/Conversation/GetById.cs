using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Modules.Chat.Application.Contracts;
using Modules.Chat.Application.DTOs.Response;

namespace Modules.Chat.Application.Queries.Conversation
{
    public sealed record GetConversationByIdQuery(Guid ConversationId) : IRequest<ConversationResponseDto>;
    public class GetConversationByIdQueryHandler(IConversationRepository conversationRepo) : IRequestHandler<GetConversationByIdQuery, ConversationResponseDto>
    {
        private readonly IConversationRepository _conversationRepo = conversationRepo;

        public async Task<ConversationResponseDto> Handle(GetConversationByIdQuery req, CancellationToken ct)
        {
            var conversation = await _conversationRepo.GetConversationByIdAsync(req.ConversationId, ct)
                ?? throw new InvalidOperationException("Conversation not found.");

            return new ConversationResponseDto(
                conversation.Id,
                conversation.UserId,
                conversation.AdminId,
                conversation.CreatedAtUtc
            );
        }
    }
}