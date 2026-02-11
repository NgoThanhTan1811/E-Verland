namespace SharedKernel;

public sealed class PageResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();

    public int TotalItems { get; init; }

    public int Page { get; init; } = 1;

    public int Limit { get; init; } = 10;

    public int TotalPages =>
        Limit <= 0 ? 0 : (int)Math.Ceiling((double)TotalItems / Limit);

    public bool HasNext => Page < TotalPages;

    public bool HasPrevious => Page > 1;
}
