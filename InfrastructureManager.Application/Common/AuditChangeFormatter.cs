using System.Text.Json;
using InfrastructureManager.Application.DTOs.Dashboard;

namespace InfrastructureManager.Application.Common;

/// <summary>
/// Turns the JSON old/new value snapshots stored on an AuditLog entry into a
/// readable list of field changes. Shared by DashboardService (recent
/// activity widget) and HistoryService (full audit trail), so the two
/// places never drift out of sync on how a change is displayed.
/// </summary>
public static class AuditChangeFormatter
{
    /// <summary>Diffs an old snapshot against a new one — used for UPDATE entries.</summary>
    public static IReadOnlyList<AuditFieldChange> ParseChanges(string? oldJson, string? newJson)
    {
        if (string.IsNullOrWhiteSpace(oldJson) || string.IsNullOrWhiteSpace(newJson))
            return Array.Empty<AuditFieldChange>();

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var oldDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(oldJson, options);
            var newDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(newJson, options);

            if (oldDict == null || newDict == null) return Array.Empty<AuditFieldChange>();

            var changes = new List<AuditFieldChange>();
            foreach (var key in newDict.Keys)
            {
                var newVal = newDict[key].ToString();
                var oldVal = oldDict.TryGetValue(key, out var oe) ? oe.ToString() : string.Empty;
                if (oldVal != newVal)
                    changes.Add(new AuditFieldChange
                    {
                        Field    = FormatFieldName(key),
                        OldValue = oldVal,
                        NewValue = newVal
                    });
            }
            return changes;
        }
        catch { return Array.Empty<AuditFieldChange>(); }
    }

    /// <summary>
    /// Flattens a single JSON snapshot into a readable list — used for
    /// CREATE/DELETE (and other non-diff) entries where there's only one
    /// side of the story to show, not a before/after.
    /// </summary>
    public static IReadOnlyList<AuditFieldChange> ParseSnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<AuditFieldChange>();

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
            if (dict == null) return Array.Empty<AuditFieldChange>();

            return dict.Select(kv => new AuditFieldChange
            {
                Field    = FormatFieldName(kv.Key),
                OldValue = string.Empty,
                NewValue = kv.Value.ToString()
            }).ToList();
        }
        catch { return Array.Empty<AuditFieldChange>(); }
    }

    public static string FormatFieldName(string name) => name switch
    {
        "IpAddress"    => "IP Address",
        "NetworkId"    => "Network",
        "LocationId"   => "Location",
        "DepartmentId" => "Department",
        "DeviceType"   => "Device Type",
        // Already a human-readable label (e.g. a DeviceTypeField.Label like
        // "IP Address" or "Vervaldatum sleutel") — leave it exactly as-is,
        // don't run it through the PascalCase splitter below.
        _ when name.Contains(' ') => name,
        _              => System.Text.RegularExpressions.Regex
                            .Replace(name, "([A-Z])", " $1").Trim()
    };
}
