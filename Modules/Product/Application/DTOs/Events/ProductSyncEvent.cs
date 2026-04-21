namespace Modules.Product.Application.DTOs.Events
{
    public sealed record ProductSyncEvent
    {
        public Guid ProductId { get; init; }
        public string EventType { get; init; } = default!; // "Created" | "Updated" | "Deleted" | "ProductModerated"
        public string Name { get; init; } = default!;
        public string Description { get; init; } = default!;
        public decimal BasePrice { get; init; }
        public decimal VirtualPrice { get; init; }
        public string Slug { get; init; } = default!;
        public string Status { get; init; } = default!;
        public Guid? BrandId { get; init; }
        public List<Guid> CategoryIds { get; init; } = [];
        public List<string> ImageUrls { get; init; } = [];
        public Dictionary<string, string> Attributes { get; init; } = [];
        public string? ModerationAction { get; init; }
        public Guid? ModeratedByAdminId { get; init; }
        public string? ModerationReason { get; init; }
        public DateTime Timestamp { get; init; }
    }
}
