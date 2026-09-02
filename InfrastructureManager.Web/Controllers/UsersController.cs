using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        var vm = new List<UserListViewModel>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            vm.Add(new UserListViewModel
            {
                Id        = u.Id,
                FirstName = u.FirstName,
                LastName  = u.LastName,
                Email     = u.Email ?? string.Empty,
                IsActive  = u.IsActive,
                Role      = roles.FirstOrDefault() ?? AppRoles.Viewer,
                CreatedAt = u.CreatedAt
            });
        }

        return View(vm);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        if (await _userManager.FindByEmailAsync(vm.Email) != null)
        {
            ModelState.AddModelError(nameof(vm.Email), "A user with this email already exists.");
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
            return View(vm);
        }

        var role = vm.IsAdmin ? AppRoles.Admin : AppRoles.Viewer;
        await _userManager.AddToRoleAsync(user, role);

        TempData["Success"] = $"User {user.FirstName} {user.LastName} created as {role}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles   = await _userManager.GetRolesAsync(user);
        var isAdmin = roles.Contains(AppRoles.Admin);

        var vm = new EditUserViewModel
        {
            Id        = user.Id,
            FirstName = user.FirstName,
            LastName  = user.LastName,
            Email     = user.Email ?? string.Empty,
            IsActive  = user.IsActive,
            IsAdmin   = isAdmin
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditUserViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _userManager.FindByIdAsync(vm.Id);
        if (user == null) return NotFound();

        var existing = await _userManager.FindByEmailAsync(vm.Email);
        if (existing != null && existing.Id != vm.Id)
        {
            ModelState.AddModelError(nameof(vm.Email), "A user with this email already exists.");
            return View(vm);
        }

        user.FirstName = vm.FirstName;
        user.LastName  = vm.LastName;
        user.Email     = vm.Email;
        user.UserName  = vm.Email;
        user.IsActive  = vm.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(vm);
        }

        // Sync role
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, vm.IsAdmin ? AppRoles.Admin : AppRoles.Viewer);

        if (!string.IsNullOrWhiteSpace(vm.NewPassword))
        {
            var token  = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(vm);
            }
        }

        TempData["Success"] = $"User {user.FirstName} {user.LastName} updated.";
        return RedirectToAction(nameof(Index));
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

        // Prevent self-deletion
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
}
