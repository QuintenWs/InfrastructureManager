using System.Security.Claims;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class UserAccessService : IUserAccessService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAccessService(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context     = context;
        _userManager = userManager;
    }

    public async Task<List<int>?> GetAccessibleLocationIdsAsync(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true) return new List<int>();
        if (user.IsInRole(AppRoles.Admin)) return null; // admins zien alles

        var userId = _userManager.GetUserId(user);
        if (userId == null) return new List<int>();

        var assigned = await _context.UserLocationAccess
            .Where(a => a.UserId == userId)
            .Select(a => a.LocationId)
            .ToListAsync();

        // Nog geen beperking ingesteld → standaard alles zichtbaar.
        return assigned.Count == 0 ? null : assigned;
    }

    public async Task<bool> CanAccessLocationAsync(ClaimsPrincipal user, int locationId)
    {
        var allowed = await GetAccessibleLocationIdsAsync(user);
        return allowed == null || allowed.Contains(locationId);
    }

    public async Task<bool> CanAccessDepartmentAsync(ClaimsPrincipal user, int departmentId)
    {
        var allowed = await GetAccessibleLocationIdsAsync(user);
        if (allowed == null) return true;

        var locationId = await _context.Departments
            .Where(d => d.Id == departmentId)
            .Select(d => d.LocationId)
            .FirstOrDefaultAsync();

        return allowed.Contains(locationId);
    }

    public async Task<List<int>> GetAssignedLocationIdsAsync(string userId)
    {
        return await _context.UserLocationAccess
            .Where(a => a.UserId == userId)
            .Select(a => a.LocationId)
            .ToListAsync();
    }

    public async Task SetAssignedLocationsAsync(string userId, IEnumerable<int> locationIds)
    {
        var existing = await _context.UserLocationAccess.Where(a => a.UserId == userId).ToListAsync();
        _context.UserLocationAccess.RemoveRange(existing);

        foreach (var id in locationIds.Distinct())
            _context.UserLocationAccess.Add(new UserLocationAccess { UserId = userId, LocationId = id });

        await _context.SaveChangesAsync();
    }
}