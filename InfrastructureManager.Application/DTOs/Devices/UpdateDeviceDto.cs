using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Application.DTOs.Devices;

public class UpdateDeviceDto
{
    public int          Id           { get; set; }
    public int          DepartmentId { get; set; }
    public int?         NetworkId    { get; set; }
    public string       Name         { get; set; } = string.Empty;
    public DeviceType   DeviceType   { get; set; }
    public DeviceStatus Status       { get; set; }
    public string?      Notes        { get; set; }
}