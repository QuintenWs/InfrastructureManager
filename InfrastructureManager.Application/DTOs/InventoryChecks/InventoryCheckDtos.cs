namespace InfrastructureManager.Application.DTOs.InventoryChecks;

public class InventoryCheckSummaryDto
{
    public int      Id              { get; set; }
    public int      DepartmentId    { get; set; }
    public string   DepartmentName  { get; set; } = string.Empty;
    public string   LocationName    { get; set; } = string.Empty;
    public string   UserDisplayName { get; set; } = string.Empty;
    public DateTime CheckDate       { get; set; }

    public int TotalCount   { get; set; }
    public int PresentCount { get; set; }
    public int MissingCount { get; set; }
}

public class InventoryCheckDetailDto : InventoryCheckSummaryDto
{
    public string? Notes { get; set; }
    public IReadOnlyList<InventoryCheckItemDto> Items { get; set; } = new List<InventoryCheckItemDto>();
}

public class InventoryCheckItemDto
{
    public int     Id         { get; set; }
    public int?    DeviceId   { get; set; }
    public string  DeviceName { get; set; } = string.Empty;
    public string  DeviceType { get; set; } = string.Empty;
    public bool    IsPresent  { get; set; }
    public string? Remark     { get; set; }
    public bool    HasPhoto   { get; set; }
}

public class CreateInventoryCheckDto
{
    public int     DepartmentId { get; set; }
    public string? Notes        { get; set; }
    public List<CreateInventoryCheckItemDto> Items { get; set; } = new();
}

public class CreateInventoryCheckItemDto
{
    public int     DeviceId  { get; set; }
    public bool    IsPresent { get; set; } = true;
    public string? Remark    { get; set; }

    public byte[]? PhotoData        { get; set; }
    public string? PhotoContentType { get; set; }
    public string? PhotoFileName    { get; set; }
}
