namespace InfrastructureManager.Web.ViewModels.Departments;

public class DepartmentListViewModel
{
    public int      Id           { get; set; }
    public string   Name         { get; set; } = string.Empty;
    public string?  Description  { get; set; }
    public string   LocationName { get; set; } = string.Empty;
    public string   Address      { get; set; } = string.Empty;
    public DateTime CreatedAt    { get; set; }

    /// <summary>Open + in-progress action items for this department — see Visits.</summary>
    public int OpenActionItemCount { get; set; }
}
