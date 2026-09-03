using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Domain.Entities;

public class Device : BaseEntity
{
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    /// <summary>Derived from Department.LocationId.</summary>
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public int? NetworkId { get; set; }
    public Network? Network { get; set; }

    public string Name { get; set; } = string.Empty;

    public DeviceType   DeviceType { get; set; }
    public DeviceStatus Status     { get; set; }

    public string? Notes { get; set; }

    public ICollection<DeviceDocument> Documents { get; set; } = new List<DeviceDocument>();

    // All type-specific properties (IP, MAC, hostname, vendor, etc.)
    // are stored here as DeviceFieldValues
    public ICollection<DeviceFieldValue>  FieldValues     { get; set; } = new List<DeviceFieldValue>();
    public ICollection<MaintenanceLog>    MaintenanceLogs { get; set; } = new List<MaintenanceLog>();
}