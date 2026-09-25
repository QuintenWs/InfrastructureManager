using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService            _dashboardService;
    private readonly AppDbContext                 _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserAccessService           _userAccessService;

    public DashboardController(
        IDashboardService            dashboardService,
        AppDbContext                 context,
        UserManager<ApplicationUser> userManager,
        IUserAccessService           userAccessService)
    {
        _dashboardService  = dashboardService;
        _context           = context;
        _userManager       = userManager;
        _userAccessService = userAccessService;
    }

    public async Task<IActionResult> Index()
    {
        var userId   = _userManager.GetUserId(User)!;
        var settings = await GetOrCreateSettingsAsync(userId);

        var allowedDepartmentIds = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);
        var allowedLocationIds   = await _userAccessService.GetAccessibleLocationIdsAsync(User);

        // De gebruiker-gekozen "standaardlocatie" is een vrije voorkeur die
        // ooit zonder validatie werd opgeslagen — hier alsnog server-side
        // controleren tegen zijn huidige toegestane locaties, want anders kan
        // iemand via die voorkeur data van een niet-toegestane locatie tonen.
        int? effectiveLocationId = settings.DefaultLocationId;
        if (effectiveLocationId.HasValue && allowedLocationIds != null &&
            !allowedLocationIds.Contains(effectiveLocationId.Value))
        {
            effectiveLocationId = null;
        }

        var data = await _dashboardService.GetDashboardAsync(
            effectiveLocationId,
            allowedDepartmentIds,
            settings.RecentDevicesCount,
            settings.RecentActivityCount);

        var locationsQuery = _context.Locations.AsQueryable();
        if (allowedLocationIds != null)
            locationsQuery = locationsQuery.Where(l => allowedLocationIds.Contains(l.Id));

        var locations = await locationsQuery
            .OrderBy(l => l.Name)
            .Select(l => new SelectListItem
            {
                Value = l.Id.ToString(),
                Text  = $"{l.Name} ({l.City})"
            })
            .ToListAsync();

        string? filteredName = null;
        if (effectiveLocationId.HasValue)
        {
            filteredName = await _context.Locations
                .Where(l => l.Id == effectiveLocationId.Value)
                .Select(l => l.Name)
                .FirstOrDefaultAsync();
        }

        var vm = new DashboardViewModel
        {
            CanViewHistory     = await _userAccessService.CanViewHistoryAsync(User),
            TotalDepartments   = data.TotalDepartments,
            TotalLocations     = data.TotalLocations,
            TotalDevices       = data.TotalDevices,
            TotalNetworks      = data.TotalNetworks,
            ActiveDevices      = data.ActiveDevices,
            OfflineDevices     = data.OfflineDevices,
            MaintenanceDevices = data.MaintenanceDevices,
            RetiredDevices     = data.RetiredDevices,
            RecentDevices      = data.RecentDevices.Select(x => new RecentDeviceViewModel
            {
                Id           = x.Id,
                Name         = x.Name,
                DeviceType   = x.DeviceType,
                Status       = x.Status,
                LocationName = x.LocationName
            }),
            RecentActivity        = data.RecentActivity,
            ExpiringItems         = data.ExpiringItems,
            OverdueVisits         = data.OverdueVisits,
            AvailableLocations    = locations,
            FilteredLocationName  = filteredName,
            Settings              = new DashboardSettingsViewModel
            {
                ShowStatCards       = settings.ShowStatCards,
                ShowDeviceStatus    = settings.ShowDeviceStatus,
                ShowRecentDevices   = settings.ShowRecentDevices,
                ShowRecentActivity  = settings.ShowRecentActivity,
                ShowExpiringItems   = settings.ShowExpiringItems,
                ShowOverdueVisits   = settings.ShowOverdueVisits,
                RecentDevicesCount  = settings.RecentDevicesCount,
                RecentActivityCount = settings.RecentActivityCount,
                DefaultLocationId   = effectiveLocationId
            }
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> SaveSettings(DashboardSettingsViewModel settings)
    {
        var userId  = _userManager.GetUserId(User)!;
        var current = await GetOrCreateSettingsAsync(userId);

        // Voorkeur valideren tegen de huidige toegestane locaties — anders
        // kan iemand hier gewoon een niet-toegestane locatie posten en zo
        // toch data daarvan op zijn dashboard krijgen.
        var allowedLocationIds  = await _userAccessService.GetAccessibleLocationIdsAsync(User);
        var requestedLocationId = settings.DefaultLocationId == 0 ? null : settings.DefaultLocationId;

        if (requestedLocationId.HasValue && allowedLocationIds != null &&
            !allowedLocationIds.Contains(requestedLocationId.Value))
        {
            requestedLocationId = null;
        }

        current.ShowStatCards      = settings.ShowStatCards;
        current.ShowDeviceStatus   = settings.ShowDeviceStatus;
        current.ShowRecentDevices  = settings.ShowRecentDevices;
        current.ShowRecentActivity = settings.ShowRecentActivity;
        current.ShowExpiringItems  = settings.ShowExpiringItems;

        // Deze drie werden hier voorheen nooit toegepast, ondanks dat de
        // view ze al postte — de instelling werd dus altijd stilzwijgend
        // genegeerd. Nu ook effectief opslaan, met eenvoudige validatie
        // tegen de toegelaten waarden uit de dropdowns.
        current.ShowOverdueVisits   = settings.ShowOverdueVisits;
        current.RecentDevicesCount  = settings.RecentDevicesCount is 5 or 10 or 15 or 25
            ? settings.RecentDevicesCount : current.RecentDevicesCount;
        current.RecentActivityCount = settings.RecentActivityCount is 5 or 10 or 20 or 50
            ? settings.RecentActivityCount : current.RecentActivityCount;

        current.DefaultLocationId  = requestedLocationId;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Dashboard preferences saved.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<UserDashboardSettings> GetOrCreateSettingsAsync(string userId)
    {
        var settings = await _context.UserDashboardSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings != null) return settings;

        settings = new UserDashboardSettings { UserId = userId };
        _context.UserDashboardSettings.Add(settings);
        await _context.SaveChangesAsync();
        return settings;
    }
}