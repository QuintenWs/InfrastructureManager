using InfrastructureManager.Application.Common;
using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Repositories;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _repository;
    private readonly IAuditService     _audit;
    private readonly AppDbContext      _context;

    public DeviceService(
        IDeviceRepository repository,
        IAuditService     audit,
        AppDbContext      context)
    {
        _repository = repository;
        _audit      = audit;
        _context    = context;
    }

    public async Task<IEnumerable<DeviceDto>> GetAllAsync(string? search = null)
    {
        var items = await _repository.SearchAsync(search);
        return items.Select(ToDto);
    }

    public async Task<DeviceDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetDetailsByIdAsync(id);
        return item == null ? null : ToDto(item);
    }

    public async Task<int> CreateAsync(CreateDeviceDto dto)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == dto.DepartmentId)
            ?? throw new ArgumentException($"Department {dto.DepartmentId} not found.");

        var entity = new Device
        {
            DepartmentId = dto.DepartmentId,
            LocationId   = department.LocationId,
            NetworkId    = dto.NetworkId,
            Name         = dto.Name,
            DeviceType   = dto.DeviceType,
            Status       = dto.Status,
            Notes        = dto.Notes
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        await _audit.LogAsync("CREATE", "Device", entity.Id, entity.Name,
            newValues: new { entity.Name, entity.DeviceType, entity.Status, entity.DepartmentId, entity.NetworkId, entity.Notes });

        return entity.Id;
    }

    public async Task UpdateAsync(UpdateDeviceDto dto)
    {
        var entity = await _repository.GetByIdAsync(dto.Id);
        if (entity == null) return;

        var old = new { entity.Name, entity.DeviceType, entity.Status, entity.NetworkId, entity.DepartmentId, entity.Notes };

        if (entity.DepartmentId != dto.DepartmentId)
        {
            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == dto.DepartmentId);
            if (dept != null) entity.LocationId = dept.LocationId;
        }

        entity.DepartmentId = dto.DepartmentId;
        entity.NetworkId    = dto.NetworkId;
        entity.Name         = dto.Name;
        entity.DeviceType   = dto.DeviceType;
        entity.Status       = dto.Status;
        entity.Notes        = dto.Notes;

        _repository.Update(entity);
        await _repository.SaveChangesAsync();

        await _audit.LogAsync("UPDATE", "Device", entity.Id, entity.Name,
            oldValues: old,
            newValues: new { entity.Name, entity.DeviceType, entity.Status, entity.NetworkId, entity.DepartmentId, entity.Notes });
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return;

        var snapshot = new { entity.Name, entity.DeviceType, entity.Status, entity.DepartmentId, entity.NetworkId, entity.Notes };
        _repository.Delete(entity);
        await _repository.SaveChangesAsync();

        await _audit.LogAsync("DELETE", "Device", id, snapshot.Name, oldValues: snapshot);
    }

    public async Task<IEnumerable<DeviceDto>> FilterAsync(DeviceFilter filter)
    {
        var items = await _repository.FilterAsync(filter);
        return items.Select(ToDto);
    }

    public async Task<PagedResult<DeviceDto>> FilterPagedAsync(DeviceFilter filter, int page, int pageSize)
    {
        var query = _context.Devices
            .Include(x => x.Department)
            .Include(x => x.Location)
            .Include(x => x.Network)
            .Include(x => x.FieldValues).ThenInclude(v => v.Field)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s) ||
                x.Department.Name.ToLower().Contains(s) ||
                x.Location.Name.ToLower().Contains(s) ||
                x.FieldValues.Any(v => v.Value.ToLower().Contains(s)));
        }

        if (filter.DeviceType.HasValue)
            query = query.Where(x => x.DeviceType == filter.DeviceType.Value);

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        if (filter.LocationId.HasValue)
            query = query.Where(x => x.LocationId == filter.LocationId.Value);

        if (filter.DepartmentId.HasValue)
            query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);

        var totalCount = await query.CountAsync();

        var entities = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entities.Select(ToDto).ToList();

        return new PagedResult<DeviceDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    private static DeviceDto ToDto(Device x)
    {
        // Try to get IP address from field values for convenience display
        var ipField = x.FieldValues
            .FirstOrDefault(v =>
                v.Field?.FieldType == "ipv4" ||
                v.Field?.FieldType == "ipv6" ||
                v.Field?.FieldType == "ip"   ||
                v.Field?.FieldKey  == "ip_address");

        return new DeviceDto
        {
            Id             = x.Id,
            DepartmentId   = x.DepartmentId,
            DepartmentName = x.Department.Name,
            LocationId     = x.LocationId,
            LocationName   = x.Location.Name,
            NetworkId      = x.NetworkId,
            NetworkName    = x.Network?.Name,
            Name           = x.Name,
            DeviceType     = x.DeviceType,
            Status         = x.Status,
            Notes          = x.Notes,
            IpAddress      = ipField?.Value
        };
    }
}