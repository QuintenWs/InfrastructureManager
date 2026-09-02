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

    /// <summary>How many days ahead of today an item is shown as "expiring soon".
    /// Already-expired items are always shown regardless of this window.</summary>
    private const int ExpiryWarningWindowDays = 30;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDto> GetDashboardAsync(int? locationId = null)
    {
        // All queries filter by locationId when provided
        var deptQuery   = _context.Departments.AsQueryable();
        var networkQuery= _context.Networks.AsQueryable();
        var deviceQuery = _context.Devices.AsQueryable();

        if (locationId.HasValue)
        {
            deptQuery    = deptQuery.Where(x => x.LocationId == locationId.Value);
            networkQuery = networkQuery.Where(x => x.LocationId == locationId.Value);
            deviceQuery  = deviceQuery.Where(x => x.LocationId == locationId.Value);
        }

        var totalDepartments   = await deptQuery.CountAsync();
        var totalLocations     = locationId.HasValue ? 1 : await _context.Locations.CountAsync();
        var totalNetworks      = await networkQuery.CountAsync();
        var totalDevices       = await deviceQuery.CountAsync();
        var activeDevices      = await deviceQuery.CountAsync(x => x.Status == DeviceStatus.Active);
        var offlineDevices     = await deviceQuery.CountAsync(x => x.Status == DeviceStatus.Offline);
        var maintenanceDevices = await deviceQuery.CountAsync(x => x.Status == DeviceStatus.Maintenance);
        var retiredDevices     = await deviceQuery.CountAsync(x => x.Status == DeviceStatus.Retired);

        var recentDevices = await deviceQuery
            .Include(x => x.Location)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new RecentDeviceDto
            {
                Id           = x.Id,
                Name         = x.Name,
                DeviceType   = x.DeviceType.ToString(),
                Status       = x.Status.ToString(),
                LocationName = x.Location.Name
            })
            .ToListAsync();

        // Activity feed always global — per-location filtering would hide cross-location changes
        var rawLogs = await _context.AuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .ToListAsync();

        var recentActivity = rawLogs.Select(x => new AuditLogDto
        {
            Id              = x.Id,
            UserDisplayName = x.UserDisplayName,
            Action          = x.Action,
            EntityType      = x.EntityType,
            EntityId        = x.EntityId,
            EntityLabel     = x.EntityLabel,
            OldValues       = x.OldValues,
            NewValues       = x.NewValues,
            CreatedAt       = x.CreatedAt,
            Changes         = AuditChangeFormatter.ParseChanges(x.OldValues, x.NewValues)
        }).ToList();

        var expiringItems = await GetExpiringItemsAsync(locationId);

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
            ExpiringItems      = expiringItems
        };
    }

    /// <summary>
    /// Generic across any device type: picks up every DeviceFieldValue whose
    /// field is a date field marked AlertOnExpiry (e.g. a crypto key's expiry
    /// date), and returns the ones already expired or expiring within
    /// ExpiryWarningWindowDays. Values are stored as free-text strings, so
    /// parsing happens in memory after a narrow, indexed-friendly filter.
    /// </summary>
    private async Task<List<ExpiringItemDto>> GetExpiringItemsAsync(int? locationId)
    {
        var candidatesQuery = _context.DeviceFieldValues
            .Where(v => v.Field.AlertOnExpiry
                     && v.Field.FieldType == "date"
                     && v.Value != "");

        if (locationId.HasValue)
            candidatesQuery = candidatesQuery.Where(v => v.Device.LocationId == locationId.Value);

        var candidates = await candidatesQuery
            .Select(v => new
            {
                v.Value,
                FieldLabel     = v.Field.Label,
                DeviceId       = v.Device.Id,
                DeviceName     = v.Device.Name,
                DepartmentName = v.Device.Department.Name,
                LocationName   = v.Device.Location.Name
            })
            .ToListAsync();

        var today = DateTime.UtcNow.Date;

        return candidates
            .Select(c => new { c, Parsed = DateTime.TryParse(c.Value, out var d) ? d.Date : (DateTime?)null })
            .Where(x => x.Parsed.HasValue)
            .Select(x => new ExpiringItemDto
            {
                DeviceId       = x.c.DeviceId,
                DeviceName     = x.c.DeviceName,
                DepartmentName = x.c.DepartmentName,
                LocationName   = x.c.LocationName,
                FieldLabel     = x.c.FieldLabel,
                ExpiryDate     = x.Parsed!.Value,
                DaysRemaining  = (x.Parsed.Value - today).Days
            })
            .Where(i => i.DaysRemaining <= ExpiryWarningWindowDays)
            .OrderBy(i => i.ExpiryDate)
            .Take(10)
            .ToList();
    }
}
