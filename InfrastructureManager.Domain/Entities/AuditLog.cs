namespace InfrastructureManager.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }

    /// <summary>ASP.NET Identity user ID (string GUID)</summary>
    public string? UserId { get; set; }

    /// <summary>Display name at time of action, e.g. "Quinten Willekens"</summary>
    public string UserDisplayName { get; set; } = string.Empty;

    /// <summary>CREATE, UPDATE, DELETE</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>e.g. "Device", "Network", "Location"</summary>
    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    /// <summary>Human-readable label, e.g. the device name</summary>
    public string EntityLabel { get; set; } = string.Empty;

    /// <summary>JSON snapshot before the change (null for CREATE)</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot after the change (null for DELETE)</summary>
    public string? NewValues { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
