using InfrastructureManager.Application.Common;
using InfrastructureManager.Application.DTOs.Dashboard;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    private const int ExpiryWarningWindowDays = 100;
    private const int OverdueVisitWarningDays = 730; // 2 jaar

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDto> GetDashboardAsync(
        int? locationId = null,
        IReadOnlyCollection<int>? allowedLocationIds = null,
        int recentDevicesCount = 5,
        int recentActivityCount = 10)
    {
        List<int>? effectiveIds = allowedLocationIds?.ToList();

        if (locationId.HasValue && (effectiveIds == null || effectiveIds.Contains(locationId.Value)))
            effectiveIds = new List<int> { locationId.Value };

        var deptQuery    = _context.Departments.AsQueryable();
        var networkQuery = _context.Networks.AsQueryable();
        var deviceQuery  = _context.Devices.AsQueryable();

        if (effectiveIds != null)
        {
            deptQuery    = deptQuery.Where(x => effectiveIds.Contains(x.LocationId));
            networkQuery = networkQuery.Where(x => effectiveIds.Contains(x.LocationId));
            deviceQuery  = deviceQuery.Where(x => effectiveIds.Contains(x.LocationId));
        }

        var totalDepartments   = await deptQuery.CountAsync();
        var totalLocations     = effectiveIds != null ? effectiveIds.Count : await _context.Locations.CountAsync();
        var totalNetworks      = await networkQuery.CountAsync();
        var totalDevices       = await deviceQuery.CountAsync();
        var activeDevices      = await deviceQuery.CountAsync(x => x.Status == DeviceStatus.Active);
        var offlineDevices     = await deviceQuery.CountAsync(x => x.Status == DeviceStatus.Offline);
        var maintenanceDevices = await deviceQuery.CountAsync(x => x.Status == DeviceStatus.Maintenance);
        var retiredDevices     = await deviceQuery.CountAsync(x => x.Status == DeviceStatus.Retired);

        var recentDevices = await deviceQuery
            .Include(x => x.Location)
            .OrderByDescending(x => x.CreatedAt)
            .Take(recentDevicesCount)
            .Select(x => new RecentDeviceDto
            {
                Id = x.Id, Name = x.Name, DeviceType = x.DeviceType.ToString(),
                Status = x.Status.ToString(), LocationName = x.Location.Name
            })
            .ToListAsync();

        // Blijft globaal — AuditLog houdt geen LocationId bij.
        var rawLogs = await _context.AuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(recentActivityCount)
            .ToListAsync();

        var recentActivity = rawLogs.Select(x => new AuditLogDto
        {
            Id = x.Id, UserDisplayName = x.UserDisplayName, Action = x.Action,
            EntityType = x.EntityType, EntityId = x.EntityId, EntityLabel = x.EntityLabel,
            OldValues = x.OldValues, NewValues = x.NewValues, CreatedAt = x.CreatedAt,
            Changes = AuditChangeFormatter.ParseChanges(x.OldValues, x.NewValues)
        }).ToList();

        return new DashboardDto
        {
            TotalDepartments   = totalDepartments,
            TotalLocations     = totalLocations,
            TotalNetworks      = totalNetworks,
            TotalDevices       = totalDevices,
            ActiveDevices      = activeDevices,
            OfflineDevices     = offlineDevices,
            MaintenanceDevices = maintenanceDevices,
            RetiredDevices     = retiredDevices,
            RecentDevices      = recentDevices,
            RecentActivity     = recentActivity,
            ExpiringItems      = await GetExpiringItemsAsync(effectiveIds),
            OverdueVisits      = await GetOverdueVisitsAsync(effectiveIds)
        };
    }

    private async Task<List<ExpiringItemDto>> GetExpiringItemsAsync(List<int>? locationIds)
    {
        var candidatesQuery = _context.DeviceFieldValues
            .Where(v => v.Field.AlertOnExpiry && v.Field.FieldType == "date" && v.Value != "");

        if (locationIds != null)
            candidatesQuery = candidatesQuery.Where(v => locationIds.Contains(v.Device.LocationId));

        var candidates = await candidatesQuery
            .Select(v => new
            {
                v.Value, FieldLabel = v.Field.Label, DeviceId = v.Device.Id, DeviceName = v.Device.Name,
                DepartmentName = v.Device.Department.Name, LocationName = v.Device.Location.Name
            })
            .ToListAsync();

        var today = DateTime.UtcNow.Date;

        return candidates
            .Select(c => new { c, Parsed = DateTime.TryParse(c.Value, out var d) ? d.Date : (DateTime?)null })
            .Where(x => x.Parsed.HasValue)
            .Select(x => new ExpiringItemDto
            {
                DeviceId = x.c.DeviceId, DeviceName = x.c.DeviceName, DepartmentName = x.c.DepartmentName,
                LocationName = x.c.LocationName, FieldLabel = x.c.FieldLabel,
                ExpiryDate = x.Parsed!.Value, DaysRemaining = (x.Parsed.Value - today).Days
            })
            .Where(i => i.DaysRemaining <= ExpiryWarningWindowDays)
            .OrderBy(i => i.ExpiryDate)
            .Take(10)
            .ToList();
    }

    /// <summary>Departementen zonder bezoek, of waarvan het laatste bezoek meer dan 2 jaar geleden is.</summary>
    private async Task<List<OverdueVisitDto>> GetOverdueVisitsAsync(List<int>? locationIds)
    {
        var query = _context.Departments.Include(d => d.Location).AsQueryable();
        if (locationIds != null)
            query = query.Where(d => locationIds.Contains(d.LocationId));

        var raw = await query
            .Select(d => new
            {
                d.Id, d.Name, LocationName = d.Location.Name,
                LastVisit = d.Visits.OrderByDescending(v => v.VisitDate).Select(v => (DateTime?)v.VisitDate).FirstOrDefault()
            })
            .ToListAsync();

        var today = DateTime.UtcNow.Date;

        return raw
            .Select(d => new { d, Days = d.LastVisit.HasValue ? (int?)(today - d.LastVisit.Value.Date).Days : null })
            .Where(x => x.Days == null || x.Days >= OverdueVisitWarningDays)
            .OrderByDescending(x => x.Days ?? int.MaxValue)
            .Select(x => new OverdueVisitDto
            {
                DepartmentId = x.d.Id, DepartmentName = x.d.Name, LocationName = x.d.LocationName,
                LastVisitDate = x.d.LastVisit, DaysSinceLastVisit = x.Days ?? int.MaxValue
            })
            .ToList();
    }
}