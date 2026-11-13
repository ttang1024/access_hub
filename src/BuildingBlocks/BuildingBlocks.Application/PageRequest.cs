namespace AccessHub.BuildingBlocks.Application;

/// <summary>Paging parameters for list queries.</summary>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page, at most <see cref="MaxPageSize"/>.</param>
public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    /// <summary>Largest allowed page size.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Number of items to skip.</summary>
    public int Skip => (Math.Max(Page, 1) - 1) * Math.Clamp(PageSize, 1, MaxPageSize);

    /// <summary>Page size clamped to the allowed range.</summary>
    public int Take => Math.Clamp(PageSize, 1, MaxPageSize);
}
