using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.Shared;
using InfrastructureManager.Web.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InfrastructureManager.Infrastructure.Data;

namespace InfrastructureManager.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserAccessService           _userAccessService;
    private readonly IDepartmentService           _departmentService;
    private readonly ILocationService             _locationService;
    private readonly AppDbContext                 _context;
    private const int PageSize = 20;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        IUserAccessService           userAccessService,
        IDepartmentService           departmentService,
        ILocationService             locationService,
        AppDbContext                 context)
    {
        _userManager       = userManager;
        _userAccessService = userAccessService;
        _departmentService = departmentService;
        _locationService   = locationService;
        _context           = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var query = _userManager.Users
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName);

        var totalCount = await query.CountAsync();

        var users = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();

        // Eén query voor de rollen van alle gebruikers op deze pagina, i.p.v.
        // voorheen één GetRolesAsync-call per gebruiker.
        var rolesByUserId = await (
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                where userIds.Contains(ur.UserId)
                select new { ur.UserId, r.Name }
            )
            .ToListAsync();

        var rolesLookup = rolesByUserId
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).FirstOrDefault() ?? AppRoles.Viewer);

        // Enkel voor niet-Admins opvragen — voor Admin is dit sowieso "alles",
        // en dit spaart onnodig werk uit in de batch-methode hieronder.
        var nonAdminUserIds = users
            .Where(u => (rolesLookup.TryGetValue(u.Id, out var r) ? r : AppRoles.Viewer) != AppRoles.Admin)
            .Select(u => u.Id)
            .ToList();

        var deptCounts = await _userAccessService.GetAccessibleDepartmentCountsAsync(nonAdminUserIds);

        var vm = users.Select(u =>
        {
            var role           = rolesLookup.TryGetValue(u.Id, out var r) ? r : AppRoles.Viewer;
            var isUnrestricted = role == AppRoles.Admin;

            return new UserListViewModel
            {
                Id             = u.Id,
                FirstName      = u.FirstName,
                LastName       = u.LastName,
                Email          = u.Email ?? string.Empty,
                IsActive       = u.IsActive,
                Role           = role,
                CreatedAt      = u.CreatedAt,
                IsUnrestricted = isUnrestricted,
                AccessibleDepartmentCount = isUnrestricted
                    ? 0
                    : (deptCounts.TryGetValue(u.Id, out var c) ? c : 0)
            };
        }).ToList();

        ViewBag.Pagination = new PaginationViewModel
        {
            CurrentPage = page,
            TotalPages  = PageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)PageSize),
            TotalCount  = totalCount,
            RouteValues = new Dictionary<string, string>()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(new CreateUserViewModel { AvailableGroups = await GetGroupsSelectListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AvailableGroups = await GetGroupsSelectListAsync();
            return View(vm);
        }

        if (await _userManager.FindByEmailAsync(vm.Email) != null)
        {
            ModelState.AddModelError(nameof(vm.Email), "A user with this email already exists.");
            vm.AvailableGroups = await GetGroupsSelectListAsync();
            return View(vm);
        }

        var user = new ApplicationUser
        {
            UserName       = vm.Email,
            Email          = vm.Email,
            FirstName      = vm.FirstName,
            LastName       = vm.LastName,
            IsActive       = vm.IsActive,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            vm.AvailableGroups = await GetGroupsSelectListAsync();
            return View(vm);
        }

        var role = NormalizeRole(vm.Role);
        await _userManager.AddToRoleAsync(user, role);
        await _userAccessService.SetUserGroupsAsync(user.Id, vm.AccessGroupIds);

        TempData["Success"] = $"User {user.FirstName} {user.LastName} created as {role}.";
        return RedirectToAction(nameof(Edit), new { id = user.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var role  = roles.FirstOrDefault() ?? AppRoles.Viewer;

        var vm = new EditUserViewModel
        {
            Id                   = user.Id,
            FirstName            = user.FirstName,
            LastName             = user.LastName,
            Email                = user.Email ?? string.Empty,
            IsActive             = user.IsActive,
            Role                 = role,
            CanViewHistory       = user.CanViewHistory,
            AccessGroupIds       = await _userAccessService.GetGroupIdsForUserAsync(id),
            AvailableGroups      = await GetGroupsSelectListAsync(),
            IndividualGrants     = await _userAccessService.GetIndividualGrantsForUserAsync(id),
            AvailableDepartments = await GetDepartmentsSelectListAsync(),
            AvailableLocations   = await GetLocationsSelectListAsync()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditUserViewModel vm)
    {
        var user = await _userManager.FindByIdAsync(vm.Id);
        if (user == null) return NotFound();

        async Task<IActionResult> ReturnWithErrorsAsync()
        {
            vm.AvailableGroups      = await GetGroupsSelectListAsync();
            vm.IndividualGrants     = await _userAccessService.GetIndividualGrantsForUserAsync(vm.Id);
            vm.AvailableDepartments = await GetDepartmentsSelectListAsync();
            vm.AvailableLocations   = await GetLocationsSelectListAsync();
            return View(vm);
        }

        if (!ModelState.IsValid) return await ReturnWithErrorsAsync();

        var existing = await _userManager.FindByEmailAsync(vm.Email);
        if (existing != null && existing.Id != vm.Id)
        {
            ModelState.AddModelError(nameof(vm.Email), "A user with this email already exists.");
            return await ReturnWithErrorsAsync();
        }

        var newRole = NormalizeRole(vm.Role);
        var currentUserId = _userManager.GetUserId(User);
        var wasAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);

        // Voorkomt dat een Admin zichzelf per ongeluk degradeert en zo
        // buiten alle beheerschermen (incl. dit scherm) terechtkomt.
        if (user.Id == currentUserId && wasAdmin && newRole != AppRoles.Admin)
        {
            ModelState.AddModelError(string.Empty, "You cannot remove your own Admin role.");
            return await ReturnWithErrorsAsync();
        }

        user.FirstName      = vm.FirstName;
        user.LastName       = vm.LastName;
        user.Email          = vm.Email;
        user.UserName       = vm.Email;
        user.IsActive       = vm.IsActive;
        user.CanViewHistory = vm.CanViewHistory;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return await ReturnWithErrorsAsync();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, newRole);

        await _userAccessService.SetUserGroupsAsync(user.Id, vm.AccessGroupIds);

        if (!string.IsNullOrWhiteSpace(vm.NewPassword))
        {
            var token  = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return await ReturnWithErrorsAsync();
            }
        }

        TempData["Success"] = $"User {user.FirstName} {user.LastName} updated.";
        return RedirectToAction(nameof(Edit), new { id = user.Id });
    }

    // ── Individuele uitzonderingen (los van groepen) ─────────────────────────

    [HttpPost]
    public async Task<IActionResult> AddDepartmentGrant(string id, int departmentId)
    {
        await _userAccessService.AddDepartmentGrantToUserAsync(id, departmentId);
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AddLocationGrant(string id, int locationId)
    {
        await _userAccessService.AddLocationGrantToUserAsync(id, locationId);
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveGrant(string id, int grantId)
    {
        await _userAccessService.RemoveIndividualGrantAsync(grantId);
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        if (user.Id == currentUserId)
        {
            TempData["Error"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        if (!user.IsActive)
        {
            // Dwingt een herwaardering van een eventuele al ingelogde sessie af
            // (zie SecurityStampValidatorOptions.ValidationInterval in Program.cs)
            // — zonder dit blijft een bestaande auth-cookie gewoon geldig tot hij
            // afloopt (standaard tot 8 uur), ook al is het account net
            // gedeactiveerd.
            await _userManager.UpdateSecurityStampAsync(user);
        }

        TempData["Success"] = user.IsActive
            ? $"{user.FirstName} {user.LastName} activated."
            : $"{user.FirstName} {user.LastName} deactivated.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        if (user.Id == currentUserId)
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = "Could not delete user: " +
                string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"{user.FirstName} {user.LastName} deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<List<SelectListItem>> GetGroupsSelectListAsync()
    {
        var groups = await _userAccessService.GetAllAccessGroupsAsync();
        return groups.Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.Name }).ToList();
    }

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

    private static string NormalizeRole(string role) => role switch
    {
        AppRoles.Admin  => AppRoles.Admin,
        AppRoles.Editor => AppRoles.Editor,
        _               => AppRoles.Viewer
    };
}