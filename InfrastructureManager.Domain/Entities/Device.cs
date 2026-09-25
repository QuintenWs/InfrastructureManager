using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Domain.Entities;

public class Device : BaseEntity
{
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public int? NetworkId { get; set; }
    public Network? Network { get; set; }

    public string Name { get; set; } = string.Empty;

    public DeviceType   DeviceType { get; set; }
    public DeviceStatus Status     { get; set; }

    public string? Notes { get; set; }

    public ICollection<DeviceDocument> Documents { get; set; } = new List<DeviceDocument>();

    public ICollection<DeviceFieldValue>  FieldValues     { get; set; } = new List<DeviceFieldValue>();
    public ICollection<MaintenanceLog>    MaintenanceLogs { get; set; } = new List<MaintenanceLog>();
}