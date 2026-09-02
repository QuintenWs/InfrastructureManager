namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// Stores the value of a DeviceTypeField for a specific Device.
/// One row per (Device, Field) pair.
/// </summary>
public class DeviceFieldValue
{
    public int Id { get; set; }

    public int DeviceId { get; set; }

    public Device Device { get; set; } = null!;

    public int DeviceTypeFieldId { get; set; }

    public DeviceTypeField Field { get; set; } = null!;

    public string Value { get; set; } = string.Empty;
}
