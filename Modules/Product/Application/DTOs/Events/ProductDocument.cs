namespace Modules.Product.Application.DTOs.Events
{
    public sealed record ProductDocument
    {
        public string Id { get; init; } = default!; // ProductId.ToString()
        public string Name { get; init; } = default!;
        public string Description { get; init; } = default!;
        public decimal BasePrice { get; init; }
        public decimal VirtualPrice { get; init; }
        public string Slug { get; init; } = default!;
        public string Status { get; init; } = default!;
        public string? BrandId { get; init; }
        public List<string> CategoryIds { get; init; } = [];
        public List<string> ImageUrls { get; init; } = [];
        public Dictionary<string, string> Attributes { get; init; } = [];
        public DateTime IndexedAtUtc { get; init; }
    }
}
