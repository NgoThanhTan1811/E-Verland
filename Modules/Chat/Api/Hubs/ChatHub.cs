using Infra.AWS.CloudWatch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Modules.Chat.Application.Contracts;

namespace Modules.Chat.Api.Hubs;

[Authorize]
public class ChatHub(ICloudWatchService cloudWatch, IConversationRepository conversationRepository) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await cloudWatch.PutMetricAsync("chat.hub.connected", 1);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await cloudWatch.PutMetricAsync("chat.hub.disconnected", 1);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(string conversationId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);

    public async Task SendMessage(string conversationId, string content)
    {
        // Ensure conversation exists (or create it) when customerId/sellerId are provided
        // For hub-level sends we work with the conversationId directly
        await cloudWatch.PutMetricAsync("chat.message.sent", 1);

        // Broadcast to group
        await Clients.Group(conversationId)
            .SendAsync("ReceiveMessage", Context.UserIdentifier, content, DateTime.UtcNow);
    }

    /// <summary>
    /// Called by clients to get or create a conversation between a customer and seller.
    /// Emits chat.conversation.created when a new conversation is created.
    /// </summary>
    public async Task<string> GetOrCreateConversation(string customerIdStr, string sellerIdStr)
    {
        if (!Guid.TryParse(customerIdStr, out var customerId) ||
            !Guid.TryParse(sellerIdStr, out var sellerId))
            throw new HubException("Invalid customerId or sellerId.");

        // Check if conversation already exists before creating
        var existing = await conversationRepository.GetConversationByUserAsync(customerId, sellerId);
        var conversation = await conversationRepository.GetOrCreateConversationAsync(customerId, sellerId);

        if (existing is null)
        {
            // A new conversation was created
            await cloudWatch.PutMetricAsync("chat.conversation.created", 1);
        }

        return conversation.Id.ToString();
    }
}
