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
    private readonly IDashboardService           _dashboardService;
    private readonly AppDbContext                _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        IDashboardService            dashboardService,
        AppDbContext                 context,
        UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _context          = context;
        _userManager      = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId   = _userManager.GetUserId(User)!;
        var settings = await GetOrCreateSettingsAsync(userId);

        // Apply the user's saved location filter
        var data = await _dashboardService.GetDashboardAsync(settings.DefaultLocationId);

        var locations = await _context.Locations
            .OrderBy(l => l.Name)
            .Select(l => new SelectListItem
            {
                Value = l.Id.ToString(),
                Text  = $"{l.Name} ({l.City})"
            })
            .ToListAsync();

        string? filteredName = null;
        if (settings.DefaultLocationId.HasValue)
        {
            filteredName = await _context.Locations
                .Where(l => l.Id == settings.DefaultLocationId.Value)
                .Select(l => l.Name)
                .FirstOrDefaultAsync();
        }

        var vm = new DashboardViewModel
        {
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
            AvailableLocations    = locations,
            FilteredLocationName  = filteredName,
            Settings              = new DashboardSettingsViewModel
            {
                ShowStatCards      = settings.ShowStatCards,
                ShowDeviceStatus   = settings.ShowDeviceStatus,
                ShowRecentDevices  = settings.ShowRecentDevices,
                ShowRecentActivity = settings.ShowRecentActivity,
                ShowExpiringItems  = settings.ShowExpiringItems,
                DefaultLocationId  = settings.DefaultLocationId
            }
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> SaveSettings(DashboardSettingsViewModel settings)
    {
        var userId  = _userManager.GetUserId(User)!;
        var current = await GetOrCreateSettingsAsync(userId);

        current.ShowStatCards      = settings.ShowStatCards;
        current.ShowDeviceStatus   = settings.ShowDeviceStatus;
        current.ShowRecentDevices  = settings.ShowRecentDevices;
        current.ShowRecentActivity = settings.ShowRecentActivity;
        current.ShowExpiringItems  = settings.ShowExpiringItems;
        current.DefaultLocationId  = settings.DefaultLocationId == 0
                                     ? null
                                     : settings.DefaultLocationId;

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