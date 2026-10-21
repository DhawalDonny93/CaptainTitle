namespace AppBackendBasic.Core.Helpers;

/// <summary>
/// Generic pagination result wrapper
/// </summary>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int? PreviousPage => HasPrevious ? Page - 1 : null;
    public int? NextPage => HasNext ? Page + 1 : null;

    public PaginatedResult()
    {
    }

    public PaginatedResult(List<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}

/// <summary>
/// Pagination parameters for API requests
/// </summary>
public class PaginationParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public int Skip => (Page - 1) * PageSize;
    public int Take => PageSize;
}

/// <summary>
/// Extension methods for pagination (non-async for Core layer)
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Creates a paginated result from a list
    /// </summary>
    public static PaginatedResult<T> ToPaginatedResult<T>(
        this List<T> items,
        int page,
        int pageSize,
        int totalCount)
    {
        return new PaginatedResult<T>(items, page, pageSize, totalCount);
    }

    /// <summary>
    /// Applies pagination to a queryable (returns IQueryable for deferred execution)
    /// </summary>
    public static IQueryable<T> Paginate<T>(
        this IQueryable<T> query,
        PaginationParams pagination)
    {
        return query
            .Skip(pagination.Skip)
            .Take(pagination.Take);
    }

    /// <summary>
    /// Applies pagination to a queryable with explicit page and pageSize
    /// </summary>
    public static IQueryable<T> Paginate<T>(
        this IQueryable<T> query,
        int page,
        int pageSize)
    {
        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }
}
