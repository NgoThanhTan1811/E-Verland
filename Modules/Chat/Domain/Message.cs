namespace Modules.Chat.Domain;

public sealed class Message
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }  

    public string Content { get; private set; } = default!;

    public DateTime SentAtUtc { get; private set; } = DateTime.UtcNow;


    private Message() { }

    public Message(Guid conversationId, Guid senderId, string content)
    {
        if (conversationId == Guid.Empty) throw new ArgumentException("ConversationId required");
        if (senderId == Guid.Empty) throw new ArgumentException("SenderId required");
        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Content required");

        ConversationId = conversationId;
        SenderId = senderId;
        Content = content.Trim();
    }

    public void Edit(string newContent)
    {
        if (Id == Guid.Empty) throw new InvalidOperationException("Message was deleted");
        Content = newContent.Trim();
    }

}