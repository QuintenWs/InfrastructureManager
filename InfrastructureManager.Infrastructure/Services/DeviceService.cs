using InfrastructureManager.Application.Common;
using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class DeviceService : IDeviceService
{
    private readonly IAuditService _audit;
    private readonly AppDbContext  _context;

    public DeviceService(IAuditService audit, AppDbContext context)
    {
        _audit   = audit;
        _context = context;
    }

    public async Task<DeviceDto?> GetByIdAsync(int id)
    {
        var item = await _context.Devices
            .Include(x => x.Department).ThenInclude(d => d.Location)
            .Include(x => x.Network)
            .Include(x => x.FieldValues).ThenInclude(v => v.Field)
            .FirstOrDefaultAsync(x => x.Id == id);

        return item == null ? null : ToDto(item);
    }

    public async Task<int> CreateAsync(CreateDeviceDto dto)
    {
        var departmentExists = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId);
        if (!departmentExists)
            throw new ArgumentException($"Department {dto.DepartmentId} not found.");

        var entity = new Device
        {
            DepartmentId = dto.DepartmentId,
            NetworkId    = dto.NetworkId,
            Name         = dto.Name,
            DeviceType   = dto.DeviceType,
            Status       = dto.Status,
            Notes        = dto.Notes
        };

        _context.Devices.Add(entity);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("CREATE", "Device", entity.Id, entity.Name,
            newValues: new { entity.Name, entity.DeviceType, entity.Status, entity.DepartmentId, entity.NetworkId, entity.Notes },
            departmentId: entity.DepartmentId);

        return entity.Id;
    }

    public async Task UpdateAsync(UpdateDeviceDto dto)
    {
        var entity = await _context.Devices.FindAsync(dto.Id);
        if (entity == null) return;

        var old = new { entity.Name, entity.DeviceType, entity.Status, entity.NetworkId, entity.DepartmentId, entity.Notes };

        // Custom-veldwaarden horen volledig bij één specifiek DeviceType — bij
        // een wissel zijn de oude waarden niet meer relevant en moeten ze weg
        // (zie de audit-uitleg bij stap 1.2 in het rapport).
        var typeChanged = entity.DeviceType != dto.DeviceType;

        entity.DepartmentId = dto.DepartmentId;
        entity.NetworkId    = dto.NetworkId;
        entity.Name         = dto.Name;
        entity.DeviceType   = dto.DeviceType;
        entity.Status       = dto.Status;
        entity.Notes        = dto.Notes;
        entity.UpdatedAt    = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (typeChanged)
        {
            await _context.DeviceFieldValues
                .Where(v => v.DeviceId == entity.Id)
                .ExecuteDeleteAsync();
        }

        await _audit.LogAsync("UPDATE", "Device", entity.Id, entity.Name,
            oldValues: old,
            newValues: new { entity.Name, entity.DeviceType, entity.Status, entity.NetworkId, entity.DepartmentId, entity.Notes },
            departmentId: entity.DepartmentId);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Devices.FindAsync(id);
        if (entity == null) return;

        var snapshot = new { entity.Name, entity.DeviceType, entity.Status, entity.DepartmentId, entity.NetworkId, entity.Notes };

        await _context.InventoryCheckItems
            .Where(i => i.DeviceId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.DeviceId, (int?)null));

        _context.Devices.Remove(entity);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("DELETE", "Device", id, snapshot.Name, oldValues: snapshot, departmentId: snapshot.DepartmentId);
    }

    public async Task<IEnumerable<DeviceDto>> FilterAsync(DeviceFilter filter)
    {
        var query = BuildFilterQuery(filter);
        var items = await query.OrderBy(x => x.Name).ToListAsync();
        return items.Select(ToDto);
    }

    public async Task<PagedResult<DeviceDto>> FilterPagedAsync(DeviceFilter filter, int page, int pageSize)
    {
        var query = BuildFilterQuery(filter);

        var totalCount = await query.CountAsync();

        var entities = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entities.Select(ToDto).ToList();

        return new PagedResult<DeviceDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    /// <summary>
    /// Eén gedeelde queryopbouw voor GetAllAsync/FilterAsync/FilterPagedAsync
    /// — zie NetworkService.BuildQuery voor dezelfde redenering.
    /// </summary>
    private IQueryable<Device> BuildFilterQuery(DeviceFilter filter)
    {
        var query = _context.Devices
            .Include(x => x.Department).ThenInclude(d => d.Location)
            .Include(x => x.Network)
            .Include(x => x.FieldValues).ThenInclude(v => v.Field)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s) ||
                x.Department.Name.ToLower().Contains(s) ||
                x.Department.Location.Name.ToLower().Contains(s) ||
                x.FieldValues.Any(v => v.Value.ToLower().Contains(s)));
        }

        if (filter.DeviceType.HasValue)
            query = query.Where(x => x.DeviceType == filter.DeviceType.Value);

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        if (filter.LocationId.HasValue)
            query = query.Where(x => x.Department.LocationId == filter.LocationId.Value);

        if (filter.DepartmentId.HasValue)
            query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);

        if (filter.AllowedDepartmentIds != null)
            query = query.Where(x => filter.AllowedDepartmentIds.Contains(x.DepartmentId));

        return query;
    }

    private static DeviceDto ToDto(Device x) => new()
    {
        Id             = x.Id,
        DepartmentId   = x.DepartmentId,
        DepartmentName = x.Department.Name,
        LocationId     = x.Department.LocationId,
        LocationName   = x.Department.Location.Name,
        NetworkId      = x.NetworkId,
        NetworkName    = x.Network?.Name,
        Name           = x.Name,
        DeviceType     = x.DeviceType,
        Status         = x.Status,
        Notes          = x.Notes,
        IpAddress      = x.GetIpAddress()
    };
}