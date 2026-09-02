namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// One inventory-check session for a department — e.g. another service
/// walking through and verifying what's physically present. Holds a
/// snapshot of every device checked (see InventoryCheckItem), so the
/// record stays meaningful even if devices are later renamed or removed.
/// </summary>
public class InventoryCheck
{
    public int Id { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    /// <summary>ASP.NET Identity user ID of the person who did the check, if logged in.</summary>
    public string? UserId { get; set; }

    /// <summary>Display name at time of check, e.g. "Quinten Willekens".</summary>
    public string UserDisplayName { get; set; } = string.Empty;

    public DateTime CheckDate { get; set; } = DateTime.UtcNow;

    /// <summary>General notes about the check as a whole.</summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InventoryCheckItem> Items { get; set; } = new List<InventoryCheckItem>();
}
