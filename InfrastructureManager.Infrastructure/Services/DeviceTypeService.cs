using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class DeviceTypeService : IDeviceTypeService
{
    private readonly AppDbContext   _context;
    private readonly IAuditService  _audit;

    public DeviceTypeService(AppDbContext context, IAuditService audit)
    {
        _context = context;
        _audit   = audit;
    }

    // ── Used by device forms ──────────────────────────────────────────────────

    public async Task<DeviceTypeDefinitionDto?> GetFieldsAsync(
        DeviceType deviceType,
        int?       deviceId = null)
    {
        var definition = await _context.DeviceTypeDefinitions
            .Include(x => x.Fields.OrderBy(f => f.SortOrder))
            .FirstOrDefaultAsync(x => x.DeviceType == deviceType);

        if (definition == null) return null;

        Dictionary<int, string> existingValues = new();
        if (deviceId.HasValue)
        {
            existingValues = await _context.DeviceFieldValues
                .Where(v => v.DeviceId == deviceId.Value)
                .ToDictionaryAsync(v => v.DeviceTypeFieldId, v => v.Value);
        }

        return MapToDto(definition, existingValues);
    }

    public async Task SaveFieldValuesAsync(
        int deviceId,
        Dictionary<int, string> fieldValues)
    {
        var existing = await _context.DeviceFieldValues
            .Include(v => v.Field)
            .Where(v => v.DeviceId == deviceId)
            .ToListAsync();

        // Needed to label brand-new values (no existing record/Field include yet)
        var fieldIds    = fieldValues.Keys.ToList();
        var fieldLabels = await _context.DeviceTypeFields
            .Where(f => fieldIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.Label);

        // Collected as we go, then logged once as a single "Device" audit
        // entry — so a device's IP/MAC/custom-field edits show up in its
        // own history with a proper old/new diff per field.
        var changedOld = new Dictionary<string, object>();
        var changedNew = new Dictionary<string, object>();

        foreach (var (fieldId, value) in fieldValues)
        {
            var record = existing.FirstOrDefault(v => v.DeviceTypeFieldId == fieldId);
            var label  = record?.Field?.Label
                         ?? (fieldLabels.TryGetValue(fieldId, out var l) ? l : $"Veld #{fieldId}");

            if (record != null)
            {
                if (record.Value != value)
                {
                    changedOld[label] = record.Value;
                    changedNew[label] = value;
                }
                record.Value = value;
            }
            else if (!string.IsNullOrWhiteSpace(value))
            {
                changedOld[label] = string.Empty;
                changedNew[label] = value;

                _context.DeviceFieldValues.Add(new DeviceFieldValue
                {
                    DeviceId          = deviceId,
                    DeviceTypeFieldId = fieldId,
                    Value             = value
                });
            }
        }

        var clearedIds = fieldValues
            .Where(kv => string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => kv.Key)
            .ToHashSet();

        var toRemove = existing.Where(v => clearedIds.Contains(v.DeviceTypeFieldId)).ToList();
        foreach (var rem in toRemove)
        {
            if (string.IsNullOrWhiteSpace(rem.Value)) continue;
            var label = rem.Field?.Label ?? "Veld";
            changedOld[label] = rem.Value;
            changedNew[label] = string.Empty;
        }

        _context.DeviceFieldValues.RemoveRange(toRemove);

        await _context.SaveChangesAsync();

        if (changedOld.Count > 0)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            await _audit.LogAsync("UPDATE", "Device", deviceId, device?.Name ?? $"Toestel #{deviceId}",
                oldValues: changedOld, newValues: changedNew);
        }
    }

    // ── Management ────────────────────────────────────────────────────────────

    public async Task<IEnumerable<DeviceTypeDefinitionDto>> GetAllDefinitionsAsync()
    {
        var definitions = await _context.DeviceTypeDefinitions
            .Include(x => x.Fields.OrderBy(f => f.SortOrder))
            .OrderBy(x => x.Name)
            .ToListAsync();

        return definitions.Select(d => MapToDto(d, new()));
    }

    public async Task<DeviceTypeDefinitionDto?> GetDefinitionByIdAsync(int id)
    {
        var definition = await _context.DeviceTypeDefinitions
            .Include(x => x.Fields.OrderBy(f => f.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id);

        return definition == null ? null : MapToDto(definition, new());
    }

    public async Task<int> CreateDefinitionAsync(string name, string? description)
    {
        // Use DeviceType.Other (99) as placeholder — the enum isn't strictly needed
        // for custom types; the definition Id is the primary key used everywhere.
        // We still need a unique DeviceType value: use the next available int > 100.
        var maxUsed = await _context.DeviceTypeDefinitions
            .MaxAsync(d => (int?)d.DeviceType) ?? 99;

        var newTypeValue = Math.Max(maxUsed + 1, 100);

        var definition = new DeviceTypeDefinition
        {
            DeviceType  = (DeviceType)newTypeValue,
            Name        = name,
            Description = description
        };

        _context.DeviceTypeDefinitions.Add(definition);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("CREATE", "DeviceTypeDefinition", definition.Id, definition.Name,
            newValues: new { definition.Name, definition.Description });

        return definition.Id;
    }

    public async Task UpdateDefinitionAsync(int id, string name, string? description)
    {
        var definition = await _context.DeviceTypeDefinitions.FindAsync(id);
        if (definition == null) return;

        var old = new { definition.Name, definition.Description };

        definition.Name        = name;
        definition.Description = description;
        await _context.SaveChangesAsync();

        await _audit.LogAsync("UPDATE", "DeviceTypeDefinition", id, definition.Name,
            oldValues: old, newValues: new { definition.Name, definition.Description });
    }

    public async Task<DeviceTypeFieldDto> AddFieldAsync(int definitionId, CreateFieldDto dto)
    {
        var maxOrder = await _context.DeviceTypeFields
            .Where(f => f.DeviceTypeDefinitionId == definitionId)
            .MaxAsync(f => (int?)f.SortOrder) ?? 0;

        // Auto-generate FieldKey from label if not provided
        var key = string.IsNullOrWhiteSpace(dto.FieldKey)
            ? GenerateKey(dto.Label)
            : dto.FieldKey.Trim().ToLower().Replace(" ", "_");

        var field = new DeviceTypeField
        {
            DeviceTypeDefinitionId = definitionId,
            Label                  = dto.Label,
            FieldKey               = key,
            FieldType              = dto.FieldType,
            SelectOptions          = dto.SelectOptions,
            IsRequired             = dto.IsRequired,
            // AlertOnExpiry only makes sense for date fields — guard here so a
            // stray posted value can never silently apply to another field type.
            AlertOnExpiry          = dto.FieldType == "date" && dto.AlertOnExpiry,
            SortOrder              = maxOrder + 1
        };

        _context.DeviceTypeFields.Add(field);
        await _context.SaveChangesAsync();

        var defName = (await _context.DeviceTypeDefinitions.FindAsync(definitionId))?.Name ?? $"Apparaattype #{definitionId}";
        await _audit.LogAsync("CREATE", "DeviceTypeDefinition", definitionId, defName,
            newValues: new { Veld = field.Label, field.FieldType, field.IsRequired, field.AlertOnExpiry, field.SelectOptions });

        return new DeviceTypeFieldDto
        {
            Id            = field.Id,
            Label         = field.Label,
            FieldKey      = field.FieldKey,
            FieldType     = field.FieldType,
            SelectOptions = field.SelectOptions,
            IsRequired    = field.IsRequired,
            AlertOnExpiry = field.AlertOnExpiry,
            SortOrder     = field.SortOrder
        };
    }

    public async Task UpdateFieldAsync(int fieldId, CreateFieldDto dto)
    {
        var field = await _context.DeviceTypeFields.FindAsync(fieldId);
        if (field == null) return;

        var old = new { Veld = field.Label, field.FieldType, field.IsRequired, field.AlertOnExpiry, field.SelectOptions };

        field.Label         = dto.Label;
        field.FieldType     = dto.FieldType;
        field.SelectOptions = dto.SelectOptions;
        field.IsRequired    = dto.IsRequired;
        field.AlertOnExpiry = dto.FieldType == "date" && dto.AlertOnExpiry;

        await _context.SaveChangesAsync();

        var defName = (await _context.DeviceTypeDefinitions.FindAsync(field.DeviceTypeDefinitionId))?.Name ?? "Apparaattype";
        await _audit.LogAsync("UPDATE", "DeviceTypeDefinition", field.DeviceTypeDefinitionId, defName,
            oldValues: old,
            newValues: new { Veld = field.Label, field.FieldType, field.IsRequired, field.AlertOnExpiry, field.SelectOptions });
    }

    public async Task DeleteFieldAsync(int fieldId)
    {
        var field = await _context.DeviceTypeFields.FindAsync(fieldId);
        if (field == null) return;

        var defId      = field.DeviceTypeDefinitionId;
        var fieldLabel = field.Label;
        var defName    = (await _context.DeviceTypeDefinitions.FindAsync(defId))?.Name ?? "Apparaattype";

        // Remove all values for this field first (NoAction FK)
        var values = await _context.DeviceFieldValues
            .Where(v => v.DeviceTypeFieldId == fieldId)
            .ToListAsync();
        _context.DeviceFieldValues.RemoveRange(values);

        _context.DeviceTypeFields.Remove(field);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("DELETE", "DeviceTypeDefinition", defId, defName,
            oldValues: new { Veld = fieldLabel });
    }

    public async Task DeleteDefinitionAsync(int id)
    {
        var definition = await _context.DeviceTypeDefinitions
            .Include(d => d.Fields)
                .ThenInclude(f => f.Values)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (definition == null) return;

        var snapshot = new { definition.Name, definition.Description };

        _context.DeviceTypeDefinitions.Remove(definition);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("DELETE", "DeviceTypeDefinition", id, snapshot.Name, oldValues: snapshot);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DeviceTypeDefinitionDto MapToDto(
        DeviceTypeDefinition            definition,
        Dictionary<int, string>         existingValues) => new()
    {
        Id   = definition.Id,
        Name = definition.Name,
        Fields = definition.Fields.Select(f => new DeviceTypeFieldDto
        {
            Id            = f.Id,
            Label         = f.Label,
            FieldKey      = f.FieldKey,
            FieldType     = f.FieldType,
            SelectOptions = f.SelectOptions,
            IsRequired    = f.IsRequired,
            AlertOnExpiry = f.AlertOnExpiry,
            SortOrder     = f.SortOrder,
            CurrentValue  = existingValues.TryGetValue(f.Id, out var val) ? val : string.Empty
        })
    };

    private static string GenerateKey(string label) =>
        System.Text.RegularExpressions.Regex
            .Replace(label.ToLower().Trim(), @"[^a-z0-9]+", "_")
            .Trim('_');
}
