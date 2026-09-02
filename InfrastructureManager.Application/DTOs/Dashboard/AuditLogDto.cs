namespace InfrastructureManager.Application.DTOs.Dashboard;

public class AuditLogDto
{
    public int    Id              { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string Action          { get; set; } = string.Empty;
    public string EntityType      { get; set; } = string.Empty;
    public int    EntityId        { get; set; }
    public string EntityLabel     { get; set; } = string.Empty;
    public string? OldValues      { get; set; }
    public string? NewValues      { get; set; }
    public DateTime CreatedAt     { get; set; }

    /// <summary>
    /// Parsed list of field-level changes for UPDATE actions.
    /// Each entry: (FieldName, OldValue, NewValue)
    /// </summary>
    public IReadOnlyList<AuditFieldChange> Changes { get; set; }
        = new List<AuditFieldChange>();
}

public class AuditFieldChange
{
    public string Field    { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}
