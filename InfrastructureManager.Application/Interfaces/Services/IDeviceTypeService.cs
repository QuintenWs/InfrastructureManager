using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IDeviceTypeService
{
    // ── Used by device create/edit forms ──────────────────────────────────────
    Task<DeviceTypeDefinitionDto?> GetFieldsAsync(
        DeviceType deviceType,
        int?       deviceId = null);

    /// <summary>
    /// Controleert IP-adresvelden in de geposte waarden op conflicten met een
    /// ander toestel op hetzelfde Network — gooit InfrastructureManager.
    /// Domain.Exceptions.IpConflictException bij een conflict, slaat zelf
    /// niets op. Roep dit aan vóór het toestel zelf wordt aangemaakt/
    /// bijgewerkt (networkId komt dan uit het formulier, niet uit een al
    /// bestaand Device-record), zodat een conflict nooit een half aangemaakt
    /// toestel (wél het toestel, niet de velden) achterlaat.
    /// excludeDeviceId: bij een Edit het bewerkte toestel zelf uitsluiten;
    /// null bij een Create (het toestel bestaat dan nog niet).
    /// </summary>
    Task ValidateFieldValuesAsync(
        int?                    networkId,
        int?                    excludeDeviceId,
        Dictionary<int, string> fieldValues);

    Task SaveFieldValuesAsync(
        int deviceId,
        Dictionary<int, string> fieldValues);

    // ── Management ────────────────────────────────────────────────────────────
    Task<IEnumerable<DeviceTypeDefinitionDto>> GetAllDefinitionsAsync();

    Task<DeviceTypeDefinitionDto?> GetDefinitionByIdAsync(int id);

    /// <summary>Creates a brand-new device type with the given name and fields.</summary>
    Task<int> CreateDefinitionAsync(string name, string? description);

    /// <summary>Updates the display name of a definition.</summary>
    Task UpdateDefinitionAsync(int id, string name, string? description);

    /// <summary>Adds a field to an existing type definition.</summary>
    Task<DeviceTypeFieldDto> AddFieldAsync(int definitionId, CreateFieldDto dto);

    /// <summary>Updates an existing field's label, type or options.</summary>
    Task UpdateFieldAsync(int fieldId, CreateFieldDto dto);

    /// <summary>Removes a field and all its device values.</summary>
    Task DeleteFieldAsync(int fieldId);

    /// <summary>Deletes an entire type definition (and all its fields/values).</summary>
    Task DeleteDefinitionAsync(int id);
}

public class CreateFieldDto
{
    public string  Label         { get; set; } = string.Empty;
    public string  FieldKey      { get; set; } = string.Empty;
    public string  FieldType     { get; set; } = "text";
    public string? SelectOptions { get; set; }
    public bool    IsRequired    { get; set; }

    /// <summary>Only applied when FieldType == "date" — see DeviceTypeField.AlertOnExpiry.</summary>
    public bool    AlertOnExpiry { get; set; }
}