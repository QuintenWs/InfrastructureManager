using InfrastructureManager.Application.DTOs.InventoryChecks;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InfrastructureManager.Application.Common;

namespace InfrastructureManager.Infrastructure.Services;

public class InventoryCheckService : IInventoryCheckService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public InventoryCheckService(
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

    public async Task<PagedResult<InventoryCheckSummaryDto>> GetByDepartmentPagedAsync(int departmentId, int page, int pageSize)
    {
        var query = _context.InventoryChecks
            .Where(c => c.DepartmentId == departmentId)
            .Include(c => c.Department).ThenInclude(d => d.Location)
            .Include(c => c.Items);

        var totalCount = await query.CountAsync();

        var checks = await query
            .OrderByDescending(c => c.CheckDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<InventoryCheckSummaryDto>
        {
            Items      = checks.Select(ToSummaryDto).ToList(),
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<PagedResult<InventoryCheckSummaryDto>> GetRecentPagedAsync(int page, int pageSize)
    {
        var query = _context.InventoryChecks
            .Include(c => c.Department).ThenInclude(d => d.Location)
            .Include(c => c.Items);

        var totalCount = await query.CountAsync();

        var checks = await query
            .OrderByDescending(c => c.CheckDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<InventoryCheckSummaryDto>
        {
            Items      = checks.Select(ToSummaryDto).ToList(),
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<IEnumerable<InventoryCheckSummaryDto>> GetByDepartmentAsync(int departmentId)
    {
        var checks = await _context.InventoryChecks
            .Where(c => c.DepartmentId == departmentId)
            .Include(c => c.Department).ThenInclude(d => d.Location)
            .Include(c => c.Items)
            .OrderByDescending(c => c.CheckDate)
            .ToListAsync();

        return checks.Select(ToSummaryDto);
    }

    public async Task<IEnumerable<InventoryCheckSummaryDto>> GetRecentAsync(int take = 10)
    {
        var checks = await _context.InventoryChecks
            .Include(c => c.Department).ThenInclude(d => d.Location)
            .Include(c => c.Items)
            .OrderByDescending(c => c.CheckDate)
            .Take(take)
            .ToListAsync();

        return checks.Select(ToSummaryDto);
    }

    public async Task<InventoryCheckDetailDto?> GetByIdAsync(int id)
    {
        var check = await _context.InventoryChecks
            .Include(c => c.Department).ThenInclude(d => d.Location)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (check == null) return null;

        var summary = ToSummaryDto(check);

        return new InventoryCheckDetailDto
        {
            Id              = summary.Id,
            DepartmentId    = summary.DepartmentId,
            DepartmentName  = summary.DepartmentName,
            LocationName    = summary.LocationName,
            UserDisplayName = summary.UserDisplayName,
            CheckDate       = summary.CheckDate,
            TotalCount      = summary.TotalCount,
            PresentCount    = summary.PresentCount,
            MissingCount    = summary.MissingCount,
            Notes           = check.Notes,
            Items = check.Items
                .OrderBy(i => i.DeviceName)
                .Select(i => new InventoryCheckItemDto
                {
                    Id         = i.Id,
                    DeviceId   = i.DeviceId,
                    DeviceName = i.DeviceName,
                    DeviceType = i.DeviceType,
                    IsPresent  = i.IsPresent,
                    Remark     = i.Remark,
                    HasPhoto   = i.PhotoData != null && i.PhotoData.Length > 0
                })
                .ToList()
        };
    }

    public async Task<int> CreateAsync(CreateInventoryCheckDto dto)
    {
        var (userId, displayName) = await GetCurrentUserAsync();

        // Snapshot the device's current name/type at the moment of the
        // check, rather than relying on a live join later — keeps this
        // historical record accurate even if the device gets renamed,
        // re-typed, or deleted afterwards.
        var deviceIds = dto.Items.Select(i => i.DeviceId).ToList();
        var devices = await _context.Devices
            .Where(d => deviceIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d);

        var check = new InventoryCheck
        {
            DepartmentId    = dto.DepartmentId,
            UserId          = userId,
            UserDisplayName = displayName,
            CheckDate       = DateTime.UtcNow,
            Notes           = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            CreatedAt       = DateTime.UtcNow
        };

        foreach (var item in dto.Items)
        {
            devices.TryGetValue(item.DeviceId, out var device);

            check.Items.Add(new InventoryCheckItem
            {
                DeviceId         = item.DeviceId,
                DeviceName       = device?.Name ?? "(onbekend toestel)",
                DeviceType       = device?.DeviceType.ToString() ?? "-",
                IsPresent        = item.IsPresent,
                Remark           = string.IsNullOrWhiteSpace(item.Remark) ? null : item.Remark.Trim(),
                PhotoData        = item.PhotoData,
                PhotoContentType = item.PhotoContentType,
                PhotoFileName    = item.PhotoFileName
            });
        }

        _context.InventoryChecks.Add(check);
        await _context.SaveChangesAsync();

        var deptName = (await _context.Departments.FindAsync(dto.DepartmentId))?.Name ?? "onbekend departement";

        await _audit.LogAsync("CREATE", "InventoryCheck", check.Id, $"Controle — {deptName} ({check.CheckDate:dd/MM/yyyy})",
            newValues: new
            {
                DepartmentId = dto.DepartmentId,
                Total   = check.Items.Count,
                Present = check.Items.Count(i => i.IsPresent),
                Missing = check.Items.Count(i => !i.IsPresent)
            });

        // One entry per device too, so a device's own history shows every
        // check it was part of — not just the check session as a whole.
        foreach (var item in check.Items.Where(i => i.DeviceId.HasValue))
        {
            await _audit.LogAsync("CHECK", "Device", item.DeviceId!.Value, item.DeviceName,
                newValues: new { item.IsPresent, item.Remark, CheckDate = check.CheckDate });
        }

        return check.Id;
    }

    public async Task<(byte[] Data, string ContentType, string FileName)?> GetPhotoAsync(int itemId)
    {
        var item = await _context.InventoryCheckItems
            .AsNoTracking()
            .Where(i => i.Id == itemId && i.PhotoData != null)
            .Select(i => new { i.PhotoData, i.PhotoContentType, i.PhotoFileName })
            .FirstOrDefaultAsync();

        if (item?.PhotoData == null) return null;

        return (item.PhotoData, item.PhotoContentType ?? "image/jpeg", item.PhotoFileName ?? "photo.jpg");
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

    private static InventoryCheckSummaryDto ToSummaryDto(InventoryCheck c) => new()
    {
        Id              = c.Id,
        DepartmentId    = c.DepartmentId,
        DepartmentName  = c.Department?.Name ?? string.Empty,
        LocationName    = c.Department?.Location?.Name ?? string.Empty,
        UserDisplayName = c.UserDisplayName,
        CheckDate       = c.CheckDate,
        TotalCount      = c.Items.Count,
        PresentCount    = c.Items.Count(i => i.IsPresent),
        MissingCount    = c.Items.Count(i => !i.IsPresent)
    };
}
