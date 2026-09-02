namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// One device's result within an InventoryCheck. DeviceName/DeviceType are
/// snapshotted at check time (not read live from Device) so the historical
/// record stays accurate even if the device is later renamed, re-typed, or
/// deleted — DeviceId is therefore nullable and kept only as a convenience
/// link back to the still-existing device, when there is one.
/// </summary>
public class InventoryCheckItem
{
    public int Id { get; set; }

    public int InventoryCheckId { get; set; }
    public InventoryCheck InventoryCheck { get; set; } = null!;

    public int?    DeviceId { get; set; }
    public Device? Device   { get; set; }

    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;

    public bool    IsPresent { get; set; } = true;
    public string? Remark    { get; set; }

    public byte[]? PhotoData        { get; set; }
    public string? PhotoContentType { get; set; }
    public string? PhotoFileName    { get; set; }
}
