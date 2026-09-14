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
        IReadOnlyCollection<int>? allowedDepartmentIds = null,
        int recentDevicesCount = 5,
        int recentActivityCount = 10)
    {
        // ── Effectieve set toegankelijke departementen ──────────────────────────
        // De isolatiegrens is het departement, niet de locatie: iemand met
        // toegang tot slechts één departement op een locatie met meerdere
        // departementen mag de andere(n) niet zien, ook niet via het dashboard.
        var isScoped = locationId.HasValue || allowedDepartmentIds != null;
        List<int>? effectiveDeptIds = null;

        if (isScoped)
        {
            var deptQuery = _context.Departments.AsQueryable();
            if (locationId.HasValue)
                deptQuery = deptQuery.Where(d => d.LocationId == locationId.Value);
            if (allowedDepartmentIds != null)
                deptQuery = deptQuery.Where(d => allowedDepartmentIds.Contains(d.Id));

            effectiveDeptIds = await deptQuery.Select(d => d.Id).ToListAsync();
        }

        var networkQuery = _context.Networks.AsQueryable();
        var deviceQuery  = _context.Devices.AsQueryable();

        if (effectiveDeptIds != null)
        {
            networkQuery = networkQuery.Where(x => effectiveDeptIds.Contains(x.DepartmentId));
            deviceQuery  = deviceQuery.Where(x => effectiveDeptIds.Contains(x.DepartmentId));
        }

        var totalDepartments = effectiveDeptIds != null
            ? effectiveDeptIds.Count
            : await _context.Departments.CountAsync();

        var totalLocations = effectiveDeptIds != null
            ? await _context.Departments.Where(d => effectiveDeptIds.Contains(d.Id))
                .Select(d => d.LocationId).Distinct().CountAsync()
            : await _context.Locations.CountAsync();

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

        // Audit-log is nu wél scopebaar via DepartmentId — enkel entries
        // zonder departement (systeembreed, bv. Import) blijven verborgen
        // zodra er een beperking geldt.
        var rawLogsQuery = _context.AuditLogs.AsQueryable();
        if (effectiveDeptIds != null)
            rawLogsQuery = rawLogsQuery.Where(a => a.DepartmentId.HasValue && effectiveDeptIds.Contains(a.DepartmentId.Value));

        var rawLogs = await rawLogsQuery
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
            ExpiringItems      = await GetExpiringItemsAsync(effectiveDeptIds),
            OverdueVisits      = await GetOverdueVisitsAsync(effectiveDeptIds)
        };
    }

    private async Task<List<ExpiringItemDto>> GetExpiringItemsAsync(List<int>? departmentIds)
    {
        var candidatesQuery = _context.DeviceFieldValues
            .Where(v => v.Field.AlertOnExpiry && v.Field.FieldType == "date" && v.Value != "");

        if (departmentIds != null)
            candidatesQuery = candidatesQuery.Where(v => departmentIds.Contains(v.Device.DepartmentId));

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
    private async Task<List<OverdueVisitDto>> GetOverdueVisitsAsync(List<int>? departmentIds)
    {
        var query = _context.Departments.Include(d => d.Location).AsQueryable();
        if (departmentIds != null)
            query = query.Where(d => departmentIds.Contains(d.Id));

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