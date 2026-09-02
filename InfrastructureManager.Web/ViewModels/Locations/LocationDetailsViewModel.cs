namespace InfrastructureManager.Web.ViewModels.Locations;

public class LocationDetailsViewModel
{
    public int      Id        { get; set; }
    public string   Name      { get; set; } = string.Empty;
    public string   City      { get; set; } = string.Empty;
    public string   Country   { get; set; } = string.Empty;
    public string?  Notes     { get; set; }
    public DateTime CreatedAt { get; set; }

    public IEnumerable<DepartmentInLocationViewModel> Departments { get; set; }
        = new List<DepartmentInLocationViewModel>();
    public IEnumerable<NetworkInLocationViewModel> Networks { get; set; }
        = new List<NetworkInLocationViewModel>();
    public IEnumerable<DeviceInLocationViewModel> Devices { get; set; }
        = new List<DeviceInLocationViewModel>();
    // Photos removed — photos now belong to Department
}
