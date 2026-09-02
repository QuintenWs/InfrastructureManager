using InfrastructureManager.Application.Common;
using InfrastructureManager.Application.DTOs.Departments;
using InfrastructureManager.Application.Interfaces.Repositories;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repository;
    private readonly IAuditService         _audit;
    private readonly AppDbContext          _context;

    public DepartmentService(
        IDepartmentRepository repository,
        IAuditService         audit,
        AppDbContext          context)
    {
        _repository = repository;
        _audit      = audit;
        _context    = context;
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllAsync(string? search = null)
    {
        var items = await _repository.SearchAsync(search);
        return items.Select(ToDto);
    }

    public async Task<PagedResult<DepartmentDto>> GetPagedAsync(string? search, int page, int pageSize)
    {
        var query = _context.Departments.Include(d => d.Location).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(s) ||
                d.Location.Name.ToLower().Contains(s) ||
                d.Location.City.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(d => d.Location.Name).ThenBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DepartmentDto
            {
                Id           = x.Id,
                LocationId   = x.LocationId,
                LocationName = x.Location.Name,
                Name         = x.Name,
                Description  = x.Description,
                Address      = x.Address,
                Notes        = x.Notes,
                CreatedAt    = x.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<DepartmentDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<IEnumerable<DepartmentDto>> GetByLocationAsync(int locationId)
    {
        var items = await _repository.GetByLocationAsync(locationId);
        return items.Select(ToDto);
    }

    public async Task<DepartmentDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetDetailsByIdAsync(id);
        return item == null ? null : ToDto(item);
    }

    public async Task<IEnumerable<DepartmentPhotoResultDto>> GetPhotosAsync(int departmentId)
    {
        return await _context.DepartmentPhotos
            .Where(p => p.DepartmentId == departmentId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new DepartmentPhotoResultDto { Id = p.Id, Caption = p.Caption })
            .ToListAsync();
    }

    public async Task<DepartmentReportDto?> GetReportAsync(int id)
    {
        var dept = await _context.Departments
            .Include(d => d.Location)
            .Include(d => d.Contacts)
            .Include(d => d.Networks)
            .Include(d => d.Devices).ThenInclude(dev => dev.Network)
            .Include(d => d.Devices).ThenInclude(dev => dev.FieldValues).ThenInclude(fv => fv.Field)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dept == null) return null;

        return new DepartmentReportDto
        {
            Id           = dept.Id,
            Name         = dept.Name,
            Address      = dept.Address,
            Description  = dept.Description,
            Notes        = dept.Notes,
            LocationName = dept.Location.Name,
            LocationCity = dept.Location.City,
            GeneratedAt  = DateTime.UtcNow,

            Contacts = dept.Contacts.OrderBy(c => c.LastName).Select(c => new ContactReportDto
            {
                FullName = $"{c.FirstName} {c.LastName}".Trim(),
                Role     = c.Role,
                Email    = c.Email,
                Phone    = c.Phone
            }),

            Networks = dept.Networks.OrderBy(n => n.Name).Select(n => new NetworkReportDto
            {
                Name                 = n.Name,
                NetworkAddress       = n.NetworkAddress,
                Cidr                 = n.Cidr,
                SubnetMask           = n.SubnetMask,
                Gateway              = n.Gateway,
                PrimaryDns           = n.PrimaryDns,
                SecondaryDns         = string.IsNullOrWhiteSpace(n.SecondaryDns) ? null : n.SecondaryDns,
                IsDhcpEnabled        = n.IsDhcpEnabled,
                DhcpRange            = n.IsDhcpEnabled && !string.IsNullOrWhiteSpace(n.DhcpRangeStart)
                                       ? $"{n.DhcpRangeStart} – {n.DhcpRangeEnd}" : null,
                IsInternetAccessible = n.IsInternetAccessible,
                VlanId               = n.VlanId,
                IspName              = n.IspName,
                DeviceCount          = dept.Devices.Count(dev => dev.NetworkId == n.Id)
            }),

            Devices = dept.Devices.OrderBy(dev => dev.Name).Select(dev => new DeviceReportDto
            {
                Id          = dev.Id,
                Name        = dev.Name,
                DeviceType  = dev.DeviceType.ToString(),
                Status      = dev.Status.ToString(),
                NetworkName = dev.Network?.Name,
                Notes       = dev.Notes,
                CustomFields = dev.FieldValues
                    .Where(fv => !string.IsNullOrWhiteSpace(fv.Value))
                    .OrderBy(fv => fv.Field.SortOrder)
                    .Select(fv => (fv.Field.Label, fv.Value))
            })
        };
    }

    public async Task CreateAsync(CreateDepartmentDto dto)
    {
        var entity = new Department
        {
            LocationId  = dto.LocationId,
            Name        = dto.Name,
            Description = dto.Description,
            Address     = dto.Address,
            Notes       = dto.Notes
        };
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        await _audit.LogAsync("CREATE", "Department", entity.Id, entity.Name,
            newValues: new { entity.Name, entity.Address, entity.LocationId, entity.Description, entity.Notes });
    }

    public async Task UpdateAsync(UpdateDepartmentDto dto)
    {
        var entity = await _repository.GetDetailsByIdAsync(dto.Id);
        if (entity == null) return;
        var old = new { entity.Name, entity.Address, entity.LocationId, entity.Description, entity.Notes };
        entity.LocationId  = dto.LocationId;
        entity.Name        = dto.Name;
        entity.Description = dto.Description;
        entity.Address     = dto.Address;
        entity.Notes       = dto.Notes;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
        await _audit.LogAsync("UPDATE", "Department", entity.Id, entity.Name,
            oldValues: old,
            newValues: new { entity.Name, entity.Address, entity.LocationId, entity.Description, entity.Notes });
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return;
        var snapshot = new { entity.Name, entity.Address, entity.LocationId, entity.Description, entity.Notes };
        _repository.Delete(entity);
        await _repository.SaveChangesAsync();
        await _audit.LogAsync("DELETE", "Department", id, snapshot.Name, oldValues: snapshot);
    }

    private static DepartmentDto ToDto(Department x) => new()
    {
        Id           = x.Id,
        LocationId   = x.LocationId,
        LocationName = x.Location.Name,
        Name         = x.Name,
        Description  = x.Description,
        Address      = x.Address,
        Notes        = x.Notes,
        CreatedAt    = x.CreatedAt
    };
}