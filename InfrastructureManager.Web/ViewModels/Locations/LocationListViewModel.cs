namespace InfrastructureManager.Web.ViewModels.Locations;

public class LocationListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int DepartmentCount { get; set; }
    public int NetworkCount { get; set; }
    public int DeviceCount { get; set; }
    public DateTime CreatedAt { get; set; }
}