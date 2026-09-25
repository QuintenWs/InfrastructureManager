using InfrastructureManager.Application.DTOs.AccessGroups;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.AccessGroups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Web.Controllers;

// Volledig dynamisch beheer van toegangsgroepen — enkel Admin kan groepen
// aanmaken/aanpassen/verwijderen en scopes/leden beheren.
[Authorize(Roles = AppRoles.Admin)]
public class AccessGroupsController : Controller
{
    private readonly IUserAccessService           _userAccessService;
    private readonly IDepartmentService           _departmentService;
    private readonly ILocationService             _locationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccessGroupsController(
        IUserAccessService           userAccessService,
        IDepartmentService           departmentService,
        ILocationService             locationService,
        UserManager<ApplicationUser> userManager)
    {
        _userAccessService = userAccessService;
        _departmentService = departmentService;
        _locationService   = locationService;
        _userManager        = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var groups = await _userAccessService.GetAllAccessGroupsAsync();

        return View(groups.Select(g => new AccessGroupListViewModel
        {
            Id                   = g.Id,
            Name                 = g.Name,
            Description          = g.Description,
            CanViewHistory       = g.CanViewHistory,
            DepartmentGrantCount = g.DepartmentGrantCount,
            LocationGrantCount   = g.LocationGrantCount,
            MemberCount          = g.MemberCount
        }));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var group = await _userAccessService.GetAccessGroupDetailsAsync(id);
        if (group == null) return NotFound();

        var memberIds = group.Members.Select(m => m.UserId).ToHashSet();

        var vm = new AccessGroupDetailsViewModel
        {
            Id             = group.Id,
            Name           = group.Name,
            Description    = group.Description,
            CanViewHistory = group.CanViewHistory,
            Grants = group.Grants.Select(g => new AccessGrantRowViewModel
            {
                Id         = g.Id,
                IsLocation = g.LocationId.HasValue,
                Label      = g.DepartmentId.HasValue
                    ? $"{g.DepartmentName} ({g.DepartmentLocationName})"
                    : $"{g.LocationName} (entire location)"
            }).ToList(),
            Members = group.Members.Select(m => new AccessGroupMemberRowViewModel
            {
                UserId      = m.UserId,
                DisplayName = m.DisplayName
            }).ToList(),
            AvailableDepartments = await GetDepartmentsSelectListAsync(),
            AvailableLocations   = await GetLocationsSelectListAsync(),
            AvailableUsers       = await GetUsersSelectListAsync(memberIds)
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateAccessGroupViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccessGroupViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        try
        {
            var id = await _userAccessService.CreateAccessGroupAsync(new CreateAccessGroupDto
            {
                Name           = vm.Name,
                Description    = vm.Description,
                CanViewHistory = vm.CanViewHistory
            });

            TempData["Success"] = $"Groep '{vm.Name}' created.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(vm.Name), ex.Message);
            return View(vm);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(nameof(vm.Name), ex.Message);
            return View(vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var group = await _userAccessService.GetAccessGroupDetailsAsync(id);
        if (group == null) return NotFound();

        return View(new EditAccessGroupViewModel
        {
            Id             = group.Id,
            Name           = group.Name,
            Description    = group.Description,
            CanViewHistory = group.CanViewHistory
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditAccessGroupViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        try
        {
            await _userAccessService.UpdateAccessGroupAsync(new UpdateAccessGroupDto
            {
                Id             = vm.Id,
                Name           = vm.Name,
                Description    = vm.Description,
                CanViewHistory = vm.CanViewHistory
            });

            TempData["Success"] = "Group updated.";
            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(vm.Name), ex.Message);
            return View(vm);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(nameof(vm.Name), ex.Message);
            return View(vm);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _userAccessService.DeleteAccessGroupAsync(id);
        TempData["Success"] = "Groep deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ── Scopes (departement of locatie) ──────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> AddDepartmentGrant(int id, int departmentId)
    {
        await _userAccessService.AddDepartmentGrantToGroupAsync(id, departmentId);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AddLocationGrant(int id, int locationId)
    {
        await _userAccessService.AddLocationGrantToGroupAsync(id, locationId);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveGrant(int id, int grantId)
    {
        await _userAccessService.RemoveGroupGrantAsync(grantId);
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── Leden ─────────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> AddMember(int id, string userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
            await _userAccessService.AddUserToGroupAsync(userId, id);

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveMember(int id, string userId)
    {
        await _userAccessService.RemoveUserFromGroupAsync(userId, id);
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<List<SelectListItem>> GetDepartmentsSelectListAsync()
    {
        var items = await _departmentService.GetAllAsync();
        return items.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text  = $"{x.Name} – {x.LocationName}"
        }).ToList();
    }

    private async Task<List<SelectListItem>> GetLocationsSelectListAsync()
    {
        var items = await _locationService.GetAllAsync();
        return items.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text  = $"{x.Name} ({x.City})"
        }).ToList();
    }

    private async Task<List<SelectListItem>> GetUsersSelectListAsync(HashSet<string> excludeIds)
    {
        var users = await _userManager.Users.ToListAsync();
        return users
            .Where(u => !excludeIds.Contains(u.Id))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new SelectListItem
            {
                Value = u.Id,
                Text  = $"{u.FirstName} {u.LastName}".Trim()
            })
            .ToList();
    }
}