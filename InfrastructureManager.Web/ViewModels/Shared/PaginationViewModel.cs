namespace InfrastructureManager.Web.ViewModels.Shared;

public class PaginationViewModel
{
    public int CurrentPage { get; set; } = 1;
    public int TotalPages  { get; set; }
    public int TotalCount  { get; set; }

    /// <summary>Action to link to — almost always "Index" on the current controller.</summary>
    public string Action { get; set; } = "Index";

    /// <summary>Every other active filter/query value that must be preserved when paging.</summary>
    public Dictionary<string, string> RouteValues { get; set; } = new();
}