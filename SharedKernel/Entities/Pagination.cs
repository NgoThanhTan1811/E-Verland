public class PageResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();
    public int TotalItems { get; init; }
    public int PageNumber { get; init; } = 1;
    public int Limit { get; init; } = 20;

    public int TotalPages => Limit <= 0 ? 0 : (int)Math.Ceiling((double)TotalItems / Limit);

    public int Skip => Limit <= 0 ? 0 : Math.Max(0, (PageNumber - 1) * Limit);
    public int Take => Limit <= 0 ? 0 : Limit;
}
