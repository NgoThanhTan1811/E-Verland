namespace SharedKernel.Pagination;

public interface IPagingFilter
{
    int Page { get; set; }
    int Limit { get; set; }
}

public record PagingFilter : IPagingFilter
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
}

public sealed class PageResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = [];

    public int TotalItems { get; init; }

    public int Page { get; init; } = 1;

    public int Limit { get; init; } = 10;

    public int TotalPages =>
        Limit <= 0 ? 0 : (int)Math.Ceiling((double)TotalItems / Limit);

    public bool HasNext => Page < TotalPages;

    public bool HasPrevious => Page > 1;
}

public static class Pagination
{
    public static (int Page, int Limit, int Skip) Normalize(this IPagingFilter? filter, int defaultLimit = 20)
    {
        var pageValue = filter?.Page ?? 1;
        var limitValue = filter?.Limit ?? defaultLimit;

        return Normalize(pageValue, limitValue, defaultLimit);
    }

    public static (int Page, int Limit, int Skip) Normalize(int page, int limit, int defaultLimit = 20)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedLimit = limit < 1 ? (defaultLimit < 1 ? 1 : defaultLimit) : limit;
        var skip = (normalizedPage - 1) * normalizedLimit;

        return (normalizedPage, normalizedLimit, skip);
    }

    public static PageResult<T> PaginationResult<T>(
        IReadOnlyCollection<T> items,
        int totalItems,
        IPagingFilter? filter,
        int defaultLimit = 20)
    {
        var (page, limit, _) = Normalize(filter, defaultLimit);

        return new PageResult<T>
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            Limit = limit
        };
    }
}
