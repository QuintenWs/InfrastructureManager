using InfrastructureManager.Application.DTOs.Devices;

namespace InfrastructureManager.Web.ViewModels.Networks;

public class NetworkDetailsViewModel
{
    public int    Id                   { get; set; }
    public string DepartmentName       { get; set; } = string.Empty;
    public string LocationName         { get; set; } = string.Empty;
    public string Name                 { get; set; } = string.Empty;
    public string NetworkAddress       { get; set; } = string.Empty;
    public string SubnetMask           { get; set; } = string.Empty;
    public int    Cidr                 { get; set; }
    public string Gateway              { get; set; } = string.Empty;
    public string PrimaryDns           { get; set; } = string.Empty;
    public string SecondaryDns         { get; set; } = string.Empty;
    public string DhcpRangeStart       { get; set; } = string.Empty;
    public string DhcpRangeEnd         { get; set; } = string.Empty;
    public bool   IsDhcpEnabled        { get; set; }
    public bool   IsInternetAccessible { get; set; }
    public int?   VlanId               { get; set; }
    public string? IspName             { get; set; }
    public string? Notes               { get; set; }
    public int    DeviceCount          { get; set; }
    public IEnumerable<DeviceDto> Devices { get; set; } = new List<DeviceDto>();
}
