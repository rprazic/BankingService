using System.ComponentModel;

namespace BankingService.Application.Common;

public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    [Description("The items on this page.")]
    public IReadOnlyList<T> Items { get; }

    [Description("Current page number (1-based).")]
    public int Page { get; }

    [Description("Number of items requested per page.")]
    public int PageSize { get; }

    [Description("Total number of items across all pages.")]
    public int TotalCount { get; }

    [Description("Total number of pages.")]
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    [Description("Whether there is a next page.")]
    public bool HasNextPage => Page < TotalPages;

    [Description("Whether there is a previous page.")]
    public bool HasPreviousPage => Page > 1;
}

public record PagedQuery(int Page = 1, int PageSize = 20);