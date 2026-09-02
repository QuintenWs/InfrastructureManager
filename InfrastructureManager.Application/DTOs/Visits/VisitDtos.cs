namespace InfrastructureManager.Application.DTOs.Visits;

public class SiteVisitDto
{
    public int      Id              { get; set; }
    public int      DepartmentId    { get; set; }
    public string    DepartmentName  { get; set; } = string.Empty;
    public string    LocationName    { get; set; } = string.Empty;
    public string    UserDisplayName { get; set; } = string.Empty;
    public DateTime  VisitDate       { get; set; }
    public string?   Summary         { get; set; }
    public DateTime  CreatedAt       { get; set; }

    public int ResolvedItemCount { get; set; }
    public int NewItemCount      { get; set; }

    /// <summary>Populated only when loading a single visit's detail.</summary>
    public IReadOnlyList<ActionItemDto> ResolvedItems { get; set; } = new List<ActionItemDto>();
    public IReadOnlyList<ActionItemDto> NewItems      { get; set; } = new List<ActionItemDto>();
}

public class ActionItemDto
{
    public int    Id             { get; set; }
    public int    DepartmentId   { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string LocationName   { get; set; } = string.Empty;
    public string Description    { get; set; } = string.Empty;

    /// <summary>Open | InProgress | Resolved</summary>
    public string Status   { get; set; } = string.Empty;

    /// <summary>Low | Normal | High</summary>
    public string Priority { get; set; } = string.Empty;

    public string   CreatedByDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt            { get; set; }

    public string?   ResolvedByDisplayName { get; set; }
    public DateTime? ResolvedAt            { get; set; }
    public string?   ResolutionNotes       { get; set; }
}

/// <summary>
/// Input for registering a new visit: which open items got resolved
/// (with an optional note each) and which new items were found.
/// </summary>
public class CreateSiteVisitDto
{
    public int     DepartmentId { get; set; }
    public string? Summary      { get; set; }

    public List<ResolvedItemInput> ResolvedItems { get; set; } = new();
    public List<NewItemInput>      NewItems      { get; set; } = new();
}

public class ResolvedItemInput
{
    public int     ActionItemId    { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class NewItemInput
{
    public string Description { get; set; } = string.Empty;

    /// <summary>Low | Normal | High</summary>
    public string Priority { get; set; } = "Normal";
}
