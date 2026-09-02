using InfrastructureManager.Application.DTOs.Visits;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class VisitService : IVisitService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public VisitService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        IAuditService audit)
    {
        _context             = context;
        _httpContextAccessor = httpContextAccessor;
        _userManager         = userManager;
        _audit               = audit;
    }

    public async Task<IEnumerable<SiteVisitDto>> GetVisitsByDepartmentAsync(int departmentId)
    {
        var visits = await _context.SiteVisits
            .Where(v => v.DepartmentId == departmentId)
            .OrderByDescending(v => v.VisitDate)
            .Select(v => new
            {
                v.Id,
                v.DepartmentId,
                v.UserDisplayName,
                v.VisitDate,
                v.Summary,
                v.CreatedAt,
                ResolvedCount = v.ResolvedItems.Count,
                NewCount      = v.CreatedItems.Count
            })
            .ToListAsync();

        return visits.Select(x => new SiteVisitDto
        {
            Id                = x.Id,
            DepartmentId      = x.DepartmentId,
            UserDisplayName   = x.UserDisplayName,
            VisitDate         = x.VisitDate,
            Summary           = x.Summary,
            CreatedAt         = x.CreatedAt,
            ResolvedItemCount = x.ResolvedCount,
            NewItemCount      = x.NewCount
        });
    }

    public async Task<SiteVisitDto?> GetVisitByIdAsync(int id)
    {
        var visit = await _context.SiteVisits
            .Include(v => v.Department).ThenInclude(d => d.Location)
            .Include(v => v.ResolvedItems)
            .Include(v => v.CreatedItems)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (visit == null) return null;

        return new SiteVisitDto
        {
            Id                = visit.Id,
            DepartmentId      = visit.DepartmentId,
            DepartmentName    = visit.Department.Name,
            LocationName      = visit.Department.Location.Name,
            UserDisplayName   = visit.UserDisplayName,
            VisitDate         = visit.VisitDate,
            Summary           = visit.Summary,
            CreatedAt         = visit.CreatedAt,
            ResolvedItemCount = visit.ResolvedItems.Count,
            NewItemCount      = visit.CreatedItems.Count,
            ResolvedItems     = visit.ResolvedItems.OrderBy(i => i.Description).Select(ToDto).ToList(),
            NewItems          = visit.CreatedItems.OrderBy(i => i.Description).Select(ToDto).ToList()
        };
    }

    public async Task<IEnumerable<ActionItemDto>> GetOpenActionItemsByDepartmentAsync(int departmentId)
    {
        var items = await _context.ActionItems
            .Where(a => a.DepartmentId == departmentId && a.Status != ActionItemStatus.Resolved)
            .Include(a => a.Department).ThenInclude(d => d.Location)
            .ToListAsync();

        return items
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.CreatedAt)
            .Select(ToDto);
    }

    public async Task<IEnumerable<ActionItemDto>> GetAllOpenActionItemsAsync(int? locationId = null)
    {
        var query = _context.ActionItems
            .Where(a => a.Status != ActionItemStatus.Resolved)
            .Include(a => a.Department).ThenInclude(d => d.Location)
            .AsQueryable();

        if (locationId.HasValue)
            query = query.Where(a => a.Department.LocationId == locationId.Value);

        var items = await query.ToListAsync();

        return items
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.CreatedAt)
            .Select(ToDto);
    }

    public async Task<int> GetOpenActionItemCountAsync(int? departmentId = null)
    {
        var query = _context.ActionItems.Where(a => a.Status != ActionItemStatus.Resolved);
        if (departmentId.HasValue)
            query = query.Where(a => a.DepartmentId == departmentId.Value);

        return await query.CountAsync();
    }

    public async Task<Dictionary<int, int>> GetOpenActionItemCountsAsync()
    {
        return await _context.ActionItems
            .Where(a => a.Status != ActionItemStatus.Resolved)
            .GroupBy(a => a.DepartmentId)
            .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DepartmentId, x => x.Count);
    }

    public async Task<int> CreateVisitAsync(CreateSiteVisitDto dto)
    {
        var (userId, displayName) = await GetCurrentUserAsync();

        var visit = new SiteVisit
        {
            DepartmentId    = dto.DepartmentId,
            UserId          = userId,
            UserDisplayName = displayName,
            VisitDate       = DateTime.UtcNow,
            Summary         = string.IsNullOrWhiteSpace(dto.Summary) ? null : dto.Summary.Trim(),
            CreatedAt       = DateTime.UtcNow
        };

        _context.SiteVisits.Add(visit);
        await _context.SaveChangesAsync(); // need visit.Id for the items below

        var deptName = (await _context.Departments.FindAsync(dto.DepartmentId))?.Name ?? "onbekend departement";
        await _audit.LogAsync("CREATE", "SiteVisit", visit.Id, $"Bezoek — {deptName} ({visit.VisitDate:dd/MM/yyyy})",
            newValues: new { DepartmentId = dto.DepartmentId, dto.Summary });

        if (dto.ResolvedItems.Count > 0)
        {
            var ids = dto.ResolvedItems.Select(r => r.ActionItemId).ToList();
            var itemsToResolve = await _context.ActionItems
                .Where(a => ids.Contains(a.Id) && a.DepartmentId == dto.DepartmentId)
                .ToListAsync();

            foreach (var input in dto.ResolvedItems)
            {
                var item = itemsToResolve.FirstOrDefault(a => a.Id == input.ActionItemId);
                if (item == null) continue;

                var oldStatus = item.Status;

                item.Status                = ActionItemStatus.Resolved;
                item.ResolvedAt            = DateTime.UtcNow;
                item.ResolvedByUserId      = userId;
                item.ResolvedByDisplayName = displayName;
                item.ResolutionNotes       = string.IsNullOrWhiteSpace(input.ResolutionNotes)
                                              ? null : input.ResolutionNotes.Trim();
                item.ResolvedDuringVisitId = visit.Id;

                await _audit.LogAsync("UPDATE", "ActionItem", item.Id, item.Description,
                    oldValues: new { Status = oldStatus.ToString() },
                    newValues: new { Status = item.Status.ToString(), item.ResolutionNotes });
            }
        }

        var newlyCreatedItems = new List<ActionItem>();
        foreach (var newItem in dto.NewItems)
        {
            if (string.IsNullOrWhiteSpace(newItem.Description)) continue;

            if (!Enum.TryParse<ActionItemPriority>(newItem.Priority, true, out var priority))
                priority = ActionItemPriority.Normal;

            var entity = new ActionItem
            {
                DepartmentId         = dto.DepartmentId,
                Description          = newItem.Description.Trim(),
                Status               = ActionItemStatus.Open,
                Priority             = priority,
                CreatedByUserId      = userId,
                CreatedByDisplayName = displayName,
                CreatedAt            = DateTime.UtcNow,
                CreatedDuringVisitId = visit.Id
            };
            _context.ActionItems.Add(entity);
            newlyCreatedItems.Add(entity);
        }

        await _context.SaveChangesAsync();

        foreach (var item in newlyCreatedItems)
        {
            await _audit.LogAsync("CREATE", "ActionItem", item.Id, item.Description,
                newValues: new { item.Description, Priority = item.Priority.ToString(), Status = item.Status.ToString() });
        }

        return visit.Id;
    }

    public async Task SetInProgressAsync(int actionItemId)
    {
        var item = await _context.ActionItems.FindAsync(actionItemId);
        if (item == null || item.Status == ActionItemStatus.Resolved) return;

        var oldStatus = item.Status;
        item.Status = ActionItemStatus.InProgress;
        await _context.SaveChangesAsync();

        await _audit.LogAsync("UPDATE", "ActionItem", item.Id, item.Description,
            oldValues: new { Status = oldStatus.ToString() },
            newValues: new { Status = item.Status.ToString() });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(string? userId, string displayName)> GetCurrentUserAsync()
    {
        var httpUser = _httpContextAccessor.HttpContext?.User;
        string? userId = null;
        string display = "Systeem";

        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            var appUser = await _userManager.GetUserAsync(httpUser);
            if (appUser != null)
            {
                userId  = appUser.Id;
                display = $"{appUser.FirstName} {appUser.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(display))
                    display = appUser.Email ?? "Onbekend";
            }
        }

        return (userId, display);
    }

    private static ActionItemDto ToDto(ActionItem a) => new()
    {
        Id                    = a.Id,
        DepartmentId          = a.DepartmentId,
        DepartmentName        = a.Department?.Name ?? string.Empty,
        LocationName          = a.Department?.Location?.Name ?? string.Empty,
        Description           = a.Description,
        Status                = a.Status.ToString(),
        Priority              = a.Priority.ToString(),
        CreatedByDisplayName  = a.CreatedByDisplayName,
        CreatedAt             = a.CreatedAt,
        ResolvedByDisplayName = a.ResolvedByDisplayName,
        ResolvedAt            = a.ResolvedAt,
        ResolutionNotes       = a.ResolutionNotes
    };
}
