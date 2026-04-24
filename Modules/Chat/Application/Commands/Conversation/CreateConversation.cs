using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Modules.Chat.Application.Contracts;

namespace Modules.Chat.Application.Commands.Conversation
{
    public sealed record CreateConversationCommand
    (Guid CustomerId, Guid SellerId) 
    : IRequest<Guid>;

    public class CreateConversationCommandHandler(IConversationRepository converRepo) : IRequestHandler<CreateConversationCommand, Guid>
    {   
        private readonly IConversationRepository _converRepo = converRepo;

        public async Task<Guid> Handle(CreateConversationCommand request, CancellationToken ct)
        {
            var conversation = await _converRepo.GetOrCreateConversationAsync(request.CustomerId, request.SellerId, ct);
            return conversation.Id;
        }
    }
    
}