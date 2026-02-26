namespace Modules.Notification.Domain;

public sealed class Notification
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid AdminId { get; private set; }

    public string Title { get; private set; } = default!;
    public string Content { get; private set; } = default!;

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public DateTime? ReadAtUtc { get; private set; }

    public bool IsRead { get; private set; }

    private Notification() { }

    public Notification(Guid userId, Guid adminId, string title, string content)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required", nameof(userId));
        if (adminId == Guid.Empty) throw new ArgumentException("AdminId is required", nameof(adminId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required", nameof(title));
        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Content is required", nameof(content));

        UserId = userId;
        AdminId = adminId;
        Title = title.Trim();
        Content = content.Trim();
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAtUtc = DateTime.UtcNow;
    }
}
