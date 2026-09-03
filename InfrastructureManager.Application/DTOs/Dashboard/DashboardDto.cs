namespace InfrastructureManager.Application.DTOs.Dashboard;

public class DashboardDto
{
    public int TotalDepartments   { get; set; }
    public int TotalLocations     { get; set; }
    public int TotalDevices       { get; set; }
    public int TotalNetworks      { get; set; }
    public int ActiveDevices      { get; set; }
    public int OfflineDevices     { get; set; }
    public int MaintenanceDevices { get; set; }
    public int RetiredDevices     { get; set; }

    public IEnumerable<RecentDeviceDto>  RecentDevices  { get; set; } = new List<RecentDeviceDto>();
    public IEnumerable<AuditLogDto>      RecentActivity { get; set; } = new List<AuditLogDto>();
    public IEnumerable<ExpiringItemDto>  ExpiringItems  { get; set; } = new List<ExpiringItemDto>();
    public IEnumerable<OverdueVisitDto> OverdueVisits { get; set; } = new List<OverdueVisitDto>();

}

/// <summary>
/// A device-level date field (e.g. a crypto key's expiry date) that is
/// expired or expiring soon. Generic across any device type/field marked
/// with DeviceTypeField.AlertOnExpiry — not specific to crypto.
/// </summary>
public class ExpiringItemDto
{
    public int      DeviceId       { get; set; }
    public string   DeviceName     { get; set; } = string.Empty;
    public string   DepartmentName { get; set; } = string.Empty;
    public string   LocationName   { get; set; } = string.Empty;
    public string   FieldLabel     { get; set; } = string.Empty;
    public DateTime ExpiryDate     { get; set; }

    /// <summary>Negative when already expired.</summary>
    public int DaysRemaining { get; set; }
}