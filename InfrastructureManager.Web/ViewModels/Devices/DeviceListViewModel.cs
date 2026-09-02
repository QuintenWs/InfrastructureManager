using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Web.ViewModels.Devices;

public class DeviceListViewModel
{
    public int    Id             { get; set; }
    public string Name           { get; set; } = string.Empty;
    public string? IpAddress     { get; set; } // from field value if exists
    public DeviceType   DeviceType { get; set; }
    public DeviceStatus Status     { get; set; }
    public string LocationName   { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
}