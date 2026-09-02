namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// A single field that belongs to a device type definition.
/// e.g. Switch → "Image Version", "Port Count", "MAC Address"
/// </summary>
public class DeviceTypeField
{
    public int Id { get; set; }

    public int DeviceTypeDefinitionId { get; set; }

    public DeviceTypeDefinition DeviceTypeDefinition { get; set; } = null!;

    /// <summary>Display label, e.g. "Image Version"</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Internal key used in the form, e.g. "image_version".
    /// Unique within a device type.
    /// </summary>
    public string FieldKey { get; set; } = string.Empty;

    /// <summary>text | number | date | select</summary>
    public string FieldType { get; set; } = "text";

    /// <summary>Comma-separated options for select fields.</summary>
    public string? SelectOptions { get; set; }

    /// <summary>Whether this field must be filled in.</summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Only relevant when FieldType == "date". When true, values of this
    /// field are picked up by the dashboard's "expiring soon" widget —
    /// e.g. a crypto key's expiry date. Generic on purpose, so any future
    /// date field (warranty, contract end date, ...) can opt in.
    /// </summary>
    public bool AlertOnExpiry { get; set; }

    /// <summary>Display order within the type's field list.</summary>
    public int SortOrder { get; set; }

    public ICollection<DeviceFieldValue> Values { get; set; }
        = new List<DeviceFieldValue>();
}