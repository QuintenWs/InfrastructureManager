using InfrastructureManager.Application.Common;
using InfrastructureManager.Application.DTOs.History;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class HistoryService : IHistoryService
{
    private readonly AppDbContext _context;

    public HistoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HistoryPageResult> SearchAsync(HistoryFilter filter, IReadOnlyCollection<int>? allowedDepartmentIds = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        // Fail-closed: entries zonder DepartmentId (systeembreed, bv. Import,
        // apparaattype-beheer) blijven verborgen zodra er een beperking geldt.
        if (allowedDepartmentIds != null)
            query = query.Where(a => a.DepartmentId.HasValue && allowedDepartmentIds.Contains(a.DepartmentId.Value));

        if (!string.IsNullOrWhiteSpace(filter.UserId))
            query = query.Where(a => a.UserId == filter.UserId);

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);

        if (filter.EntityId.HasValue)
            query = query.Where(a => a.EntityId == filter.EntityId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(a => a.EntityLabel.Contains(s));
        }

        if (filter.FromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.FromDate.Value.Date);

        if (filter.ToDate.HasValue)
        {
            var toExclusive = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(a => a.CreatedAt < toExclusive);
        }

        var totalCount = await query.CountAsync();

        var page     = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? 25 : filter.PageSize;

        var rawItems = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = rawItems.Select(a => new HistoryEntryDto
        {
            Id              = a.Id,
            UserDisplayName = a.UserDisplayName,
            Action          = a.Action,
            EntityType      = a.EntityType,
            EntityId        = a.EntityId,
            EntityLabel     = a.EntityLabel,
            CreatedAt       = a.CreatedAt,
            Changes = a.Action switch
            {
                "UPDATE" => AuditChangeFormatter.ParseChanges(a.OldValues, a.NewValues),
                "CREATE" => AuditChangeFormatter.ParseSnapshot(a.NewValues),
                "DELETE" => AuditChangeFormatter.ParseSnapshot(a.OldValues),
                _        => AuditChangeFormatter.ParseSnapshot(a.NewValues ?? a.OldValues)
            }
        }).ToList();

        return new HistoryPageResult
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<IEnumerable<string>> GetEntityTypesAsync(IReadOnlyCollection<int>? allowedDepartmentIds = null)
    {
        var query = _context.AuditLogs.AsQueryable();
        if (allowedDepartmentIds != null)
            query = query.Where(a => a.DepartmentId.HasValue && allowedDepartmentIds.Contains(a.DepartmentId.Value));

        return await query
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<IEnumerable<(string UserId, string DisplayName)>> GetUsersAsync(IReadOnlyCollection<int>? allowedDepartmentIds = null)
    {
        var query = _context.AuditLogs.Where(a => a.UserId != null);
        if (allowedDepartmentIds != null)
            query = query.Where(a => a.DepartmentId.HasValue && allowedDepartmentIds.Contains(a.DepartmentId.Value));

        var raw = await query
            .Select(a => new { a.UserId, a.UserDisplayName })
            .Distinct()
            .ToListAsync();

        return raw
            .GroupBy(x => x.UserId)
            .Select(g => (g.Key!, g.First().UserDisplayName))
            .OrderBy(x => x.Item2)
            .ToList();
    }
}