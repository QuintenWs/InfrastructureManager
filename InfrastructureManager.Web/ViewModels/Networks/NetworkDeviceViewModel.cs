using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Web.ViewModels.Networks;

public class NetworkDeviceViewModel
{
    public int Id { get; set; }

    public string Name { get; set; }
        = string.Empty;

    public string Hostname { get; set; }
        = string.Empty;

    public string IpAddress { get; set; }
        = string.Empty;

    public DeviceType DeviceType { get; set; }

    public DeviceStatus Status { get; set; }
}