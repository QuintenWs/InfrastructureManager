using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Application.DTOs.Devices;

public class DeviceDto
{
    public int    Id             { get; set; }
    public int    DepartmentId   { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int    LocationId     { get; set; }
    public string LocationName   { get; set; } = string.Empty;
    public int?   NetworkId      { get; set; }
    public string? NetworkName   { get; set; }
    public string Name           { get; set; } = string.Empty;
    public DeviceType   DeviceType { get; set; }
    public DeviceStatus Status     { get; set; }
    public string? Notes          { get; set; }
 
    // Populated from DeviceFieldValues when needed
    public string? IpAddress    { get; set; }  // convenience — read from field value "ip_address"
}
