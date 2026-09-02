namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// A technician's visit to a department. Captures general notes for that
/// visit; the concrete open/resolved work items live on ActionItem and
/// link back here via CreatedDuringVisitId / ResolvedDuringVisitId.
/// </summary>
public class SiteVisit
{
    public int Id { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    /// <summary>ASP.NET Identity user ID of the technician, if logged in.</summary>
    public string? UserId { get; set; }

    /// <summary>Display name at time of visit, e.g. "Quinten Willekens".</summary>
    public string UserDisplayName { get; set; } = string.Empty;

    public DateTime VisitDate { get; set; } = DateTime.UtcNow;

    /// <summary>General notes about the visit, separate from individual action items.</summary>
    public string? Summary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Action items that were marked resolved during this visit.</summary>
    public ICollection<ActionItem> ResolvedItems { get; set; } = new List<ActionItem>();

    /// <summary>Action items that were newly reported during this visit.</summary>
    public ICollection<ActionItem> CreatedItems { get; set; } = new List<ActionItem>();
}
