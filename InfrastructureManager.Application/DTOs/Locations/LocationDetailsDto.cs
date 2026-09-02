using InfrastructureManager.Application.DTOs.Devices;

namespace InfrastructureManager.Application.DTOs.Locations;

public class LocationDetailsDto
{
    public int      Id        { get; set; }
    public string   Name      { get; set; } = string.Empty;
    public string   City      { get; set; } = string.Empty;
    public string   Country   { get; set; } = string.Empty;
    public string?  Notes     { get; set; }
    public DateTime CreatedAt { get; set; }

    public IEnumerable<DepartmentSummaryDto> Departments { get; set; } = new List<DepartmentSummaryDto>();
    public IEnumerable<NetworkSummaryDto>    Networks    { get; set; } = new List<NetworkSummaryDto>();
    public IEnumerable<DeviceDto>            Devices     { get; set; } = new List<DeviceDto>();
    // Photos removed — photos now belong to Department
}

public class DepartmentSummaryDto
{
    public int    Id           { get; set; }
    public string Name         { get; set; } = string.Empty;
    public string Address      { get; set; } = string.Empty;
    public int    ContactCount { get; set; }
}

public class NetworkSummaryDto
{
    public int    Id             { get; set; }
    public string Name           { get; set; } = string.Empty;
    public string NetworkAddress { get; set; } = string.Empty;
    public int    Cidr           { get; set; }
    public int    DeviceCount    { get; set; }
}
