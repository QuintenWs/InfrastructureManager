using InfrastructureManager.Application.Common;
using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Domain.Exceptions;
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

    public async Task ValidateFieldValuesAsync(
        int?                    networkId,
        int?                    excludeDeviceId,
        Dictionary<int, string> fieldValues)
    {
        var fieldIds = fieldValues.Keys.ToList();
        var fields = await _context.DeviceTypeFields
            .Where(f => fieldIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f);

        // Enkel relevant bij een Edit (excludeDeviceId is dan het toestel zelf) —
        // laat toe om waarden die niemand écht wijzigt over te slaan, zodat
        // legacy-data van vóór deze validatie bestond een verder ongerelateerde
        // bewerking niet blokkeert.
        var existingValues = excludeDeviceId.HasValue
            ? await _context.DeviceFieldValues
                .Where(v => v.DeviceId == excludeDeviceId.Value)
                .ToDictionaryAsync(v => v.DeviceTypeFieldId, v => v.Value)
            : new Dictionary<int, string>();

        foreach (var (fieldId, value) in fieldValues)
        {
            if (!fields.TryGetValue(fieldId, out var field)) continue;

            var isUnchanged = existingValues.TryGetValue(fieldId, out var existingValue) && existingValue == value;
            if (!isUnchanged)
                ValidateFieldFormat(field, value);

            if (networkId == null) continue;
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (!DeviceFieldHelpers.IsIpAddressField(field)) continue;

            var conflictingDeviceName = await FindConflictingDeviceNameAsync(networkId.Value, excludeDeviceId, value);
            if (conflictingDeviceName != null)
                throw new IpConflictException(value, conflictingDeviceName);
        }
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
        var fieldIds   = fieldValues.Keys.ToList();
        var fieldsById = await _context.DeviceTypeFields
            .Where(f => fieldIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f);
        
        // toevoegen, net vóór "var device = await _context.Devices.FindAsync(deviceId);"
        // Format-validatie — veiligheidsnet voor rechtstreekse aanroepers (bv.
        // DevToolsController). Slaat waarden over die ongewijzigd zijn t.o.v. wat al
        // opgeslagen staat — zie de toelichting bij ValidateFieldValuesAsync hierboven.
        foreach (var (fieldId, value) in fieldValues)
        {
            if (!fieldsById.TryGetValue(fieldId, out var field)) continue;

            var currentRecord = existing.FirstOrDefault(v => v.DeviceTypeFieldId == fieldId);
            var isUnchanged   = currentRecord != null && currentRecord.Value == value;
            if (!isUnchanged)
                ValidateFieldFormat(field, value);
        }

        var device = await _context.Devices.FindAsync(deviceId);

        // ── IP conflict check ────────────────────────────────────────────────
        // Safety net for direct callers of this method (e.g. from
        // DevToolsController) — the normal UI flow (DevicesController) already
        // calls ValidateFieldValuesAsync before the device itself is created/
        // updated, precisely to avoid a conflict surfacing here after the
        // device has already been committed.
        if (device?.NetworkId != null)
        {
            foreach (var (fieldId, value) in fieldValues)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (!fieldsById.TryGetValue(fieldId, out var field) || !DeviceFieldHelpers.IsIpAddressField(field))
                    continue;

                var conflictingDeviceName = await FindConflictingDeviceNameAsync(device.NetworkId.Value, deviceId, value);
                if (conflictingDeviceName != null)
                    throw new IpConflictException(value, conflictingDeviceName);
            }
        }

        var fieldLabels = fieldsById.ToDictionary(kv => kv.Key, kv => kv.Value.Label);

        var changedOld = new Dictionary<string, object>();
        var changedNew = new Dictionary<string, object>();

        foreach (var (fieldId, value) in fieldValues)
        {
            var record = existing.FirstOrDefault(v => v.DeviceTypeFieldId == fieldId);
            var label  = record?.Field?.Label
                         ?? (fieldLabels.TryGetValue(fieldId, out var l) ? l : $"Field #{fieldId}");

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
            var label = rem.Field?.Label ?? "Field";
            changedOld[label] = rem.Value;
            changedNew[label] = string.Empty;
        }

        _context.DeviceFieldValues.RemoveRange(toRemove);

        await _context.SaveChangesAsync();

        if (changedOld.Count > 0)
        {
            await _audit.LogAsync("UPDATE", "Device", deviceId, device?.Name ?? $"Device #{deviceId}",
                oldValues: changedOld, newValues: changedNew, departmentId: device?.DepartmentId);
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

        var key = string.IsNullOrWhiteSpace(dto.FieldKey)
            ? GenerateKey(dto.Label)
            : dto.FieldKey.Trim().ToLower().Replace(" ", "_");

        var keyExists = await _context.DeviceTypeFields
            .AnyAsync(f => f.DeviceTypeDefinitionId == definitionId && f.FieldKey == key);
        if (keyExists)
            throw new InvalidOperationException(
                $"A field with key '{key}' already exists on this device type. Choose a different label.");

        var field = new DeviceTypeField
        {
            DeviceTypeDefinitionId = definitionId,
            Label                  = dto.Label,
            FieldKey               = key,
            FieldType              = dto.FieldType,
            SelectOptions          = dto.SelectOptions,
            IsRequired             = dto.IsRequired,
            AlertOnExpiry          = dto.FieldType == "date" && dto.AlertOnExpiry,
            SortOrder              = maxOrder + 1
        };

        _context.DeviceTypeFields.Add(field);
        await _context.SaveChangesAsync();

        var defName = (await _context.DeviceTypeDefinitions.FindAsync(definitionId))?.Name ?? $"Device Type #{definitionId}";
        await _audit.LogAsync("CREATE", "DeviceTypeDefinition", definitionId, defName,
            newValues: new { Field = field.Label, field.FieldType, field.IsRequired, field.AlertOnExpiry, field.SelectOptions });

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

        var old = new { Field = field.Label, field.FieldType, field.IsRequired, field.AlertOnExpiry, field.SelectOptions };

        field.Label         = dto.Label;
        field.FieldType     = dto.FieldType;
        field.SelectOptions = dto.SelectOptions;
        field.IsRequired    = dto.IsRequired;
        field.AlertOnExpiry = dto.FieldType == "date" && dto.AlertOnExpiry;

        await _context.SaveChangesAsync();

        var defName = (await _context.DeviceTypeDefinitions.FindAsync(field.DeviceTypeDefinitionId))?.Name ?? "Device Type";
        await _audit.LogAsync("UPDATE", "DeviceTypeDefinition", field.DeviceTypeDefinitionId, defName,
            oldValues: old,
            newValues: new { Field = field.Label, field.FieldType, field.IsRequired, field.AlertOnExpiry, field.SelectOptions });
    }

    public async Task DeleteFieldAsync(int fieldId)
    {
        var field = await _context.DeviceTypeFields.FindAsync(fieldId);
        if (field == null) return;

        var defId      = field.DeviceTypeDefinitionId;
        var fieldLabel = field.Label;
        var defName    = (await _context.DeviceTypeDefinitions.FindAsync(defId))?.Name ?? "Device Type";

        var values = await _context.DeviceFieldValues
            .Where(v => v.DeviceTypeFieldId == fieldId)
            .ToListAsync();
        _context.DeviceFieldValues.RemoveRange(values);

        _context.DeviceTypeFields.Remove(field);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("DELETE", "DeviceTypeDefinition", defId, defName,
            oldValues: new { Field = fieldLabel });
    }

    public async Task DeleteDefinitionAsync(int id)
    {
        var definition = await _context.DeviceTypeDefinitions
            .Include(d => d.Fields)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (definition == null) return;

        // There is no real FK between Device.DeviceType and
        // DeviceTypeDefinition (only the same enum value) — without this
        // check, deleting would always succeed but leave existing devices of
        // this type "orphaned".
        var deviceCount = await _context.Devices.CountAsync(d => d.DeviceType == definition.DeviceType);
        if (deviceCount > 0)
        {
            var suffix = deviceCount == 1 ? "1 device" : $"{deviceCount} devices";
            throw new InvalidOperationException(
                $"'{definition.Name}' cannot be deleted: there {(deviceCount == 1 ? "is still" : "are still")} {suffix} of this type. " +
                "Delete or change those devices to a different type first.");
        }

        var fieldIds = definition.Fields.Select(f => f.Id).ToList();
        if (fieldIds.Count > 0)
        {
            await _context.DeviceFieldValues
                .Where(v => fieldIds.Contains(v.DeviceTypeFieldId))
                .ExecuteDeleteAsync();
        }

        var snapshot = new { definition.Name, definition.Description };

        _context.DeviceTypeDefinitions.Remove(definition);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("DELETE", "DeviceTypeDefinition", id, snapshot.Name, oldValues: snapshot);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string?> FindConflictingDeviceNameAsync(int networkId, int? excludeDeviceId, string value)
    {
        var query = _context.DeviceFieldValues
            .Where(v =>
                v.Device.NetworkId == networkId &&
                v.Value == value &&
                (v.Field.FieldType == "ipv4" || v.Field.FieldType == "ipv6" || v.Field.FieldKey == "ip_address"));

        if (excludeDeviceId.HasValue)
            query = query.Where(v => v.DeviceId != excludeDeviceId.Value);

        return await query.Select(v => v.Device.Name).FirstOrDefaultAsync();
    }

    private static DeviceTypeDefinitionDto MapToDto(
        DeviceTypeDefinition            definition,
        Dictionary<int, string>         existingValues) => new()
    {
        Id         = definition.Id,
        DeviceType = definition.DeviceType,
        Name       = definition.Name,
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

    private static void ValidateFieldFormat(DeviceTypeField field, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (field.IsRequired)
                throw new DeviceFieldValidationException($"'{field.Label}' is required.");
            return;
        }

        switch (field.FieldType)
        {
            case "number":
                if (!double.TryParse(value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out _))
                    throw new DeviceFieldValidationException($"'{field.Label}' must be a number.");
                break;

            case "date":
                if (!DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out _))
                    throw new DeviceFieldValidationException($"'{field.Label}' must be a valid date.");
                break;

            case "checkbox":
                if (value != "true" && value != "false")
                    throw new DeviceFieldValidationException($"'{field.Label}' must be true or false.");
                break;

            case "ipv4":
                if (!System.Net.IPAddress.TryParse(value, out var ipv4) ||
                    ipv4.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                    throw new DeviceFieldValidationException($"'{field.Label}' must be a valid IPv4 address.");
                break;

            case "ipv6":
                if (!System.Net.IPAddress.TryParse(value, out var ipv6) ||
                    ipv6.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
                    throw new DeviceFieldValidationException($"'{field.Label}' must be a valid IPv6 address.");
                break;

            case "mac":
                if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^([0-9A-Fa-f]{2}[:\-]){5}[0-9A-Fa-f]{2}$"))
                    throw new DeviceFieldValidationException($"'{field.Label}' must be a valid MAC address (AA:BB:CC:DD:EE:FF).");
                break;

            case "url":
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    throw new DeviceFieldValidationException($"'{field.Label}' must be a valid URL.");
                break;

            case "select":
                if (!string.IsNullOrWhiteSpace(field.SelectOptions))
                {
                    var options = field.SelectOptions.Split(',', StringSplitOptions.TrimEntries);
                    if (!options.Contains(value))
                        throw new DeviceFieldValidationException($"'{field.Label}' must be one of: {field.SelectOptions}.");
                }
                break;

            // "text" en "textarea" aanvaarden elke niet-lege waarde — niets te checken.
        }
    }
}