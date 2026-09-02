namespace InfrastructureManager.Domain.Entities;

public class UserDashboardSettings
{
    public int    Id     { get; set; }
    public string UserId { get; set; } = string.Empty;

    public bool ShowStatCards      { get; set; } = true;
    public bool ShowDeviceStatus   { get; set; } = true;
    public bool ShowRecentDevices  { get; set; } = true;
    public bool ShowRecentActivity { get; set; } = true;
    public bool ShowExpiringItems  { get; set; } = true;

    /// <summary>If set, dashboard stats are filtered to this location.</summary>
    public int? DefaultLocationId { get; set; }
}