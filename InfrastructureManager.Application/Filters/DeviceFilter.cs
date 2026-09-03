using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Application.Filters;

public class DeviceFilter
{
    public string?      Search       { get; set; }
    public DeviceType?  DeviceType   { get; set; }
    public DeviceStatus? Status      { get; set; }
    public int?         LocationId   { get; set; }
    public int?         DepartmentId { get; set; }
    public IReadOnlyCollection<int>? AllowedLocationIds { get; set; }
}
