using AppBackendBasic.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AppBackendBasic.Infrastructure.Extensions;

/// <summary>
/// Extension methods for IQueryable with Entity Framework Core support
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Creates a paginated result from a queryable source (async with EF Core)
    /// </summary>
    public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<T>(items, page, pageSize, totalCount);
    }

    /// <summary>
    /// Creates a paginated result from a queryable source using PaginationParams
    /// </summary>
    public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> query,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        return await query.ToPaginatedResultAsync(
            pagination.Page,
            pagination.PageSize,
            cancellationToken);
    }
}
