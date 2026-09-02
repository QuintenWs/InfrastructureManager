namespace InfrastructureManager.Web.ViewModels.Search;

public class GlobalSearchViewModel
{
    public string Query { get; set; } = string.Empty;
    public IReadOnlyList<SearchResultViewModel> Results { get; set; }
        = new List<SearchResultViewModel>();
}

public class SearchResultViewModel
{
    public string  Category    { get; set; } = string.Empty; // Device, Network, Location, Department
    public string  Icon        { get; set; } = string.Empty;
    public int     Id          { get; set; }
    public string  Title       { get; set; } = string.Empty;
    public string  Subtitle    { get; set; } = string.Empty;
    public string  Detail      { get; set; } = string.Empty;
    public string  Controller  { get; set; } = string.Empty;
    public string  Action      { get; set; } = string.Empty;
}
