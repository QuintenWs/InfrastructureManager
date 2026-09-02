namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// A timestamped note on a device — e.g. "Replaced power supply",
/// "Firmware updated to v2.1", "Cable re-seated".
/// Does not change any device data fields.
/// </summary>
public class MaintenanceLog
{
    public int Id { get; set; }

    public int DeviceId { get; set; }

    public Device Device { get; set; } = null!;

    /// <summary>ASP.NET Identity user ID who added the entry.</summary>
    public string? UserId { get; set; }

    /// <summary>Display name at time of entry, e.g. "Quinten Willekens".</summary>
    public string UserDisplayName { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
