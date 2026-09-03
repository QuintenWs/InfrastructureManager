using InfrastructureManager.Application.DTOs.Dashboard;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InfrastructureManager.Web.ViewModels.Dashboard;

public class DashboardViewModel
{
    public int TotalDepartments   { get; set; }
    public int TotalLocations     { get; set; }
    public int TotalDevices       { get; set; }
    public int TotalNetworks      { get; set; }
    public int ActiveDevices      { get; set; }
    public int OfflineDevices     { get; set; }
    public int MaintenanceDevices { get; set; }
    public int RetiredDevices     { get; set; }

    public IEnumerable<RecentDeviceViewModel> RecentDevices  { get; set; } = new List<RecentDeviceViewModel>();
    public IEnumerable<AuditLogDto>           RecentActivity { get; set; } = new List<AuditLogDto>();
    public IEnumerable<ExpiringItemDto>       ExpiringItems  { get; set; } = new List<ExpiringItemDto>();
    public DashboardSettingsViewModel         Settings       { get; set; } = new();
    public IEnumerable<OverdueVisitDto>       OverdueVisits { get; set; } = new List<OverdueVisitDto>();


    /// <summary>For the location filter dropdown in the customize panel.</summary>
    public IEnumerable<SelectListItem> AvailableLocations { get; set; } = new List<SelectListItem>();

    /// <summary>Name of the active location filter, if any.</summary>
    public string? FilteredLocationName { get; set; }
}

public class DashboardSettingsViewModel
{
    public bool ShowStatCards       { get; set; } = true;
    public bool ShowDeviceStatus    { get; set; } = true;
    public bool ShowRecentDevices   { get; set; } = true;
    public bool ShowRecentActivity  { get; set; } = true;
    public bool ShowExpiringItems   { get; set; } = true;
    public int? DefaultLocationId   { get; set; }
    public bool ShowOverdueVisits   { get; set; } = true;
    public int  RecentDevicesCount  { get; set; } = 5;
    public int  RecentActivityCount { get; set; } = 10;
}