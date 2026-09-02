namespace InfrastructureManager.Application.Common;

/// <summary>
/// Generic wrapper for any paginated query result. Used by every list-style
/// service method so pagination works the same way everywhere in the app.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page       { get; set; } = 1;
    public int PageSize   { get; set; } = 20;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}