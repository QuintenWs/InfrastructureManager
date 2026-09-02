using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// Defines a device type and its set of custom fields.
/// Pre-seeded for all known types, but can be extended via UI.
/// </summary>
public class DeviceTypeDefinition
{
    public int Id { get; set; }

    /// <summary>Matches the DeviceType enum value.</summary>
    public DeviceType DeviceType { get; set; }

    /// <summary>Display name, e.g. "Switch", "Router RED"</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<DeviceTypeField> Fields { get; set; }
        = new List<DeviceTypeField>();
}
