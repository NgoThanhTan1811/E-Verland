using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Modules.Chat.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public async Task JoinConversation(string conversationId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);

    public async Task SendMessage(string conversationId, string content)
    {
        // Broadcast tới group
        await Clients.Group(conversationId)
            .SendAsync("ReceiveMessage", Context.UserIdentifier, content, DateTime.UtcNow);
    }
}
