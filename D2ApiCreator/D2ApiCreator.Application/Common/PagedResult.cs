namespace D2ApiCreator.Application.Common;

/// <summary>
/// Wrapper for paged results
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Items in the current page
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
