namespace InfrastructureManager.Application.DTOs.Devices;

public class DeviceTypeFieldDto
{
    public int    Id            { get; set; }
    public string Label        { get; set; } = string.Empty;
    public string FieldKey     { get; set; } = string.Empty;
    public string FieldType    { get; set; } = "text";
    public string? SelectOptions { get; set; }
    public bool   IsRequired   { get; set; }

    /// <summary>Only meaningful when FieldType == "date". See DeviceTypeField.AlertOnExpiry.</summary>
    public bool   AlertOnExpiry { get; set; }

    public int    SortOrder    { get; set; }
    public string CurrentValue { get; set; } = string.Empty;
}

public class DeviceTypeDefinitionDto
{
    public int    Id         { get; set; }
    public string Name       { get; set; } = string.Empty;
    public IEnumerable<DeviceTypeFieldDto> Fields { get; set; }
        = new List<DeviceTypeFieldDto>();
}