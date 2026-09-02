using InfrastructureManager.Application.DTOs.Dashboard;

namespace InfrastructureManager.Application.DTOs.History;

public class HistoryFilter
{
    public string? UserId     { get; set; }
    public string? EntityType { get; set; }
    public int?    EntityId   { get; set; }

    /// <summary>Free-text match against EntityLabel (device name, department name, ...).</summary>
    public string? Search { get; set; }

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate   { get; set; }

    public int Page     { get; set; } = 1;
    public int PageSize  { get; set; } = 25;
}

public class HistoryEntryDto
{
    public int      Id              { get; set; }
    public string   UserDisplayName { get; set; } = string.Empty;
    public string   Action          { get; set; } = string.Empty;
    public string   EntityType      { get; set; } = string.Empty;
    public int      EntityId        { get; set; }
    public string   EntityLabel     { get; set; } = string.Empty;
    public DateTime CreatedAt       { get; set; }

    public IReadOnlyList<AuditFieldChange> Changes { get; set; } = new List<AuditFieldChange>();
}

public class HistoryPageResult
{
    public IReadOnlyList<HistoryEntryDto> Items { get; set; } = new List<HistoryEntryDto>();
    public int TotalCount { get; set; }
    public int Page       { get; set; }
    public int PageSize   { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
