using System.Security.Claims;
using InfrastructureManager.Application.DTOs.AccessGroups;
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

    // ── Effectieve leesscope ─────────────────────────────────────────────────

    public async Task<List<int>?> GetAccessibleDepartmentIdsAsync(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true) return new List<int>();
        if (user.IsInRole(AppRoles.Admin)) return null; // onbeperkt

        var userId = _userManager.GetUserId(user);
        if (userId == null) return new List<int>();

        return await ResolveAccessibleDepartmentIdsAsync(userId);
    }

    public async Task<List<int>?> GetAccessibleLocationIdsAsync(ClaimsPrincipal user)
    {
        var deptIds = await GetAccessibleDepartmentIdsAsync(user);
        if (deptIds == null) return null; // onbeperkt
        if (deptIds.Count == 0) return new List<int>();

        return await _context.Departments
            .Where(d => deptIds.Contains(d.Id))
            .Select(d => d.LocationId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<bool> CanAccessDepartmentAsync(ClaimsPrincipal user, int departmentId)
    {
        var allowed = await GetAccessibleDepartmentIdsAsync(user);
        return allowed == null || allowed.Contains(departmentId);
    }

    public async Task<bool> CanAccessLocationAsync(ClaimsPrincipal user, int locationId)
    {
        var allowed = await GetAccessibleLocationIdsAsync(user);
        return allowed == null || allowed.Contains(locationId);
    }

    // ── Schrijfscope (Editor) ────────────────────────────────────────────────

    public async Task<bool> CanEditDepartmentAsync(ClaimsPrincipal user, int departmentId)
    {
        if (user.IsInRole(AppRoles.Admin)) return true;
        if (!user.IsInRole(AppRoles.Editor)) return false; // Viewer mag nooit schrijven

        return await CanAccessDepartmentAsync(user, departmentId);
    }

    // ── Geschiedenis ──────────────────────────────────────────────────────────

    public async Task<bool> CanViewHistoryAsync(ClaimsPrincipal user)
    {
        if (user.IsInRole(AppRoles.Admin)) return true;

        var userId = _userManager.GetUserId(user);
        if (userId == null) return false;

        return await GetCanViewHistoryForUserAsync(userId);
    }

    public async Task<bool> GetCanViewHistoryForUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user?.CanViewHistory == true) return true;

        var groupIds = await _context.UserAccessGroups
            .Where(m => m.UserId == userId)
            .Select(m => m.AccessGroupId)
            .ToListAsync();

        if (groupIds.Count == 0) return false;

        return await _context.AccessGroups
            .Where(g => groupIds.Contains(g.Id))
            .AnyAsync(g => g.CanViewHistory);
    }

    public async Task SetCanViewHistoryForUserAsync(string userId, bool canViewHistory)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        user.CanViewHistory = canViewHistory;
        await _userManager.UpdateAsync(user);
    }

    // ── Admin-scherm helpers ─────────────────────────────────────────────────

    public async Task<bool> IsUnrestrictedAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        return await _userManager.IsInRoleAsync(user, AppRoles.Admin);
    }

    public async Task<List<int>> GetAccessibleDepartmentIdsForUserAsync(string userId)
    {
        if (await IsUnrestrictedAsync(userId))
            return await _context.Departments.Select(d => d.Id).ToListAsync();

        return await ResolveAccessibleDepartmentIdsAsync(userId);
    }

    public async Task<int> GetAccessibleDepartmentCountAsync(string userId)
    {
        var ids = await GetAccessibleDepartmentIdsForUserAsync(userId);
        return ids.Count;
    }

    // ── Groepenbeheer ─────────────────────────────────────────────────────────

    public async Task<List<AccessGroupSummaryDto>> GetAllAccessGroupsAsync()
    {
        return await _context.AccessGroups
            .OrderBy(g => g.Name)
            .Select(g => new AccessGroupSummaryDto
            {
                Id                   = g.Id,
                Name                 = g.Name,
                Description          = g.Description,
                CanViewHistory       = g.CanViewHistory,
                DepartmentGrantCount = g.Grants.Count(x => x.DepartmentId != null),
                LocationGrantCount   = g.Grants.Count(x => x.LocationId != null),
                MemberCount          = g.Members.Count
            })
            .ToListAsync();
    }

    public async Task<AccessGroupDetailsDto?> GetAccessGroupDetailsAsync(int id)
    {
        var group = await _context.AccessGroups
            .Include(g => g.Grants).ThenInclude(x => x.Department!).ThenInclude(d => d.Location)
            .Include(g => g.Grants).ThenInclude(x => x.Location)
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null) return null;

        var members = new List<AccessGroupMemberDto>();
        foreach (var m in group.Members)
        {
            var u = await _userManager.FindByIdAsync(m.UserId);
            members.Add(new AccessGroupMemberDto
            {
                UserId      = m.UserId,
                DisplayName = u != null ? $"{u.FirstName} {u.LastName}".Trim() : "(onbekende gebruiker)"
            });
        }

        return new AccessGroupDetailsDto
        {
            Id                   = group.Id,
            Name                 = group.Name,
            Description          = group.Description,
            CanViewHistory       = group.CanViewHistory,
            DepartmentGrantCount = group.Grants.Count(x => x.DepartmentId != null),
            LocationGrantCount   = group.Grants.Count(x => x.LocationId != null),
            MemberCount          = group.Members.Count,
            Grants  = group.Grants.Select(ToGrantDto)
                .OrderBy(x => x.DepartmentName ?? x.LocationName).ToList(),
            Members = members.OrderBy(m => m.DisplayName).ToList()
        };
    }

    public async Task<int> CreateAccessGroupAsync(CreateAccessGroupDto dto)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Groepsnaam is verplicht.");

        if (await _context.AccessGroups.AnyAsync(g => g.Name == name))
            throw new InvalidOperationException($"Er bestaat al een groep met de naam '{name}'.");

        var group = new AccessGroup
        {
            Name           = name,
            Description    = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            CanViewHistory = dto.CanViewHistory,
            CreatedAt      = DateTime.UtcNow
        };

        _context.AccessGroups.Add(group);
        await _context.SaveChangesAsync();
        return group.Id;
    }

    public async Task UpdateAccessGroupAsync(UpdateAccessGroupDto dto)
    {
        var group = await _context.AccessGroups.FindAsync(dto.Id);
        if (group == null) return;

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Groepsnaam is verplicht.");

        if (await _context.AccessGroups.AnyAsync(g => g.Name == name && g.Id != dto.Id))
            throw new InvalidOperationException($"Er bestaat al een groep met de naam '{name}'.");

        group.Name           = name;
        group.Description    = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        group.CanViewHistory = dto.CanViewHistory;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAccessGroupAsync(int id)
    {
        var group = await _context.AccessGroups.FindAsync(id);
        if (group == null) return;

        _context.AccessGroups.Remove(group); // cascadeert naar Grants + Members
        await _context.SaveChangesAsync();
    }

    public async Task AddDepartmentGrantToGroupAsync(int accessGroupId, int departmentId)
    {
        var exists = await _context.AccessGroupGrants
            .AnyAsync(g => g.AccessGroupId == accessGroupId && g.DepartmentId == departmentId);
        if (exists) return;

        _context.AccessGroupGrants.Add(new AccessGroupGrant
        {
            AccessGroupId = accessGroupId,
            DepartmentId  = departmentId
        });
        await _context.SaveChangesAsync();
    }

    public async Task AddLocationGrantToGroupAsync(int accessGroupId, int locationId)
    {
        var exists = await _context.AccessGroupGrants
            .AnyAsync(g => g.AccessGroupId == accessGroupId && g.LocationId == locationId);
        if (exists) return;

        _context.AccessGroupGrants.Add(new AccessGroupGrant
        {
            AccessGroupId = accessGroupId,
            LocationId    = locationId
        });
        await _context.SaveChangesAsync();
    }

    public async Task RemoveGroupGrantAsync(int grantId)
    {
        var grant = await _context.AccessGroupGrants.FindAsync(grantId);
        if (grant == null) return;

        _context.AccessGroupGrants.Remove(grant);
        await _context.SaveChangesAsync();
    }

    // ── Groepslidmaatschap ────────────────────────────────────────────────────

    public async Task<List<int>> GetGroupIdsForUserAsync(string userId)
    {
        return await _context.UserAccessGroups
            .Where(m => m.UserId == userId)
            .Select(m => m.AccessGroupId)
            .ToListAsync();
    }

    public async Task SetUserGroupsAsync(string userId, IEnumerable<int> accessGroupIds)
    {
        var existing = await _context.UserAccessGroups.Where(m => m.UserId == userId).ToListAsync();
        _context.UserAccessGroups.RemoveRange(existing);

        foreach (var groupId in accessGroupIds.Distinct())
            _context.UserAccessGroups.Add(new UserAccessGroup { UserId = userId, AccessGroupId = groupId });

        await _context.SaveChangesAsync();
    }

    public async Task AddUserToGroupAsync(string userId, int accessGroupId)
    {
        var exists = await _context.UserAccessGroups
            .AnyAsync(m => m.UserId == userId && m.AccessGroupId == accessGroupId);
        if (exists) return;

        _context.UserAccessGroups.Add(new UserAccessGroup { UserId = userId, AccessGroupId = accessGroupId });
        await _context.SaveChangesAsync();
    }

    public async Task RemoveUserFromGroupAsync(string userId, int accessGroupId)
    {
        var membership = await _context.UserAccessGroups
            .FirstOrDefaultAsync(m => m.UserId == userId && m.AccessGroupId == accessGroupId);
        if (membership == null) return;

        _context.UserAccessGroups.Remove(membership);
        await _context.SaveChangesAsync();
    }

    // ── Individuele uitzonderingen ────────────────────────────────────────────

    public async Task<List<AccessGrantDto>> GetIndividualGrantsForUserAsync(string userId)
    {
        var grants = await _context.UserAccessGrants
            .Include(g => g.Department!).ThenInclude(d => d.Location)
            .Include(g => g.Location)
            .Where(g => g.UserId == userId)
            .ToListAsync();

        return grants.Select(ToGrantDto)
            .OrderBy(x => x.DepartmentName ?? x.LocationName)
            .ToList();
    }

    public async Task AddDepartmentGrantToUserAsync(string userId, int departmentId)
    {
        var exists = await _context.UserAccessGrants
            .AnyAsync(g => g.UserId == userId && g.DepartmentId == departmentId);
        if (exists) return;

        _context.UserAccessGrants.Add(new UserAccessGrant { UserId = userId, DepartmentId = departmentId });
        await _context.SaveChangesAsync();
    }

    public async Task AddLocationGrantToUserAsync(string userId, int locationId)
    {
        var exists = await _context.UserAccessGrants
            .AnyAsync(g => g.UserId == userId && g.LocationId == locationId);
        if (exists) return;

        _context.UserAccessGrants.Add(new UserAccessGrant { UserId = userId, LocationId = locationId });
        await _context.SaveChangesAsync();
    }

    public async Task RemoveIndividualGrantAsync(int grantId)
    {
        var grant = await _context.UserAccessGrants.FindAsync(grantId);
        if (grant == null) return;

        _context.UserAccessGrants.Remove(grant);
        await _context.SaveChangesAsync();
    }

    // ── Interne resolutie ────────────────────────────────────────────────────

    /// <summary>
    /// Bouwt de effectieve set toegankelijke DepartmentId's op uit (a) alle
    /// scopes van de groepen waarin de gebruiker zit, en (b) zijn individuele
    /// uitzonderingen. Een LocationId-scope wordt live uitgebreid naar alle
    /// departementen die er nu bij horen (geen momentopname).
    /// </summary>
    private async Task<List<int>> ResolveAccessibleDepartmentIdsAsync(string userId)
    {
        var directDepartmentIds = new HashSet<int>();
        var locationIds         = new HashSet<int>();

        var groupIds = await _context.UserAccessGroups
            .Where(m => m.UserId == userId)
            .Select(m => m.AccessGroupId)
            .ToListAsync();

        if (groupIds.Count > 0)
        {
            var groupGrants = await _context.AccessGroupGrants
                .Where(g => groupIds.Contains(g.AccessGroupId))
                .Select(g => new { g.DepartmentId, g.LocationId })
                .ToListAsync();

            foreach (var g in groupGrants)
            {
                if (g.DepartmentId.HasValue) directDepartmentIds.Add(g.DepartmentId.Value);
                if (g.LocationId.HasValue)   locationIds.Add(g.LocationId.Value);
            }
        }

        var individualGrants = await _context.UserAccessGrants
            .Where(g => g.UserId == userId)
            .Select(g => new { g.DepartmentId, g.LocationId })
            .ToListAsync();

        foreach (var g in individualGrants)
        {
            if (g.DepartmentId.HasValue) directDepartmentIds.Add(g.DepartmentId.Value);
            if (g.LocationId.HasValue)   locationIds.Add(g.LocationId.Value);
        }

        if (locationIds.Count > 0)
        {
            var deptIdsForLocations = await _context.Departments
                .Where(d => locationIds.Contains(d.LocationId))
                .Select(d => d.Id)
                .ToListAsync();

            foreach (var id in deptIdsForLocations) directDepartmentIds.Add(id);
        }

        return directDepartmentIds.ToList();
    }

    private static AccessGrantDto ToGrantDto(AccessGroupGrant g) => new()
    {
        Id                     = g.Id,
        DepartmentId           = g.DepartmentId,
        DepartmentName         = g.Department?.Name,
        DepartmentLocationName = g.Department?.Location?.Name,
        LocationId             = g.LocationId,
        LocationName           = g.Location?.Name
    };

    private static AccessGrantDto ToGrantDto(UserAccessGrant g) => new()
    {
        Id                     = g.Id,
        DepartmentId           = g.DepartmentId,
        DepartmentName         = g.Department?.Name,
        DepartmentLocationName = g.Department?.Location?.Name,
        LocationId             = g.LocationId,
        LocationName           = g.Location?.Name
    };
}