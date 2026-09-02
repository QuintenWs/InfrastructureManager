using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// A to-do item for a department — something a technician noticed that
/// either got fixed on the spot or needs attention on a future visit.
/// Deliberately department-level only (not tied to a specific device),
/// per design decision.
/// </summary>
public class ActionItem
{
    public int Id { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public ActionItemStatus   Status   { get; set; } = ActionItemStatus.Open;
    public ActionItemPriority Priority { get; set; } = ActionItemPriority.Normal;

    public string?  CreatedByUserId      { get; set; }
    public string   CreatedByDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt            { get; set; } = DateTime.UtcNow;

    /// <summary>The visit during which this item was first reported, if any.</summary>
    public int?       CreatedDuringVisitId { get; set; }
    public SiteVisit?  CreatedDuringVisit   { get; set; }

    public string?   ResolvedByUserId      { get; set; }
    public string?   ResolvedByDisplayName { get; set; }
    public DateTime? ResolvedAt            { get; set; }
    public string?   ResolutionNotes       { get; set; }

    /// <summary>The visit during which this item was marked resolved, if any.</summary>
    public int?       ResolvedDuringVisitId { get; set; }
    public SiteVisit?  ResolvedDuringVisit   { get; set; }
}
