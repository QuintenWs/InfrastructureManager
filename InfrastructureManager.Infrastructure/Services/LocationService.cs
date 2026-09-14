using InfrastructureManager.Application.Common;
using InfrastructureManager.Application.DTOs.Locations;
using InfrastructureManager.Application.Interfaces.Repositories;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _repository;
    private readonly IAuditService       _audit;
    private readonly AppDbContext        _context;

    public LocationService(
        ILocationRepository repository,
        IAuditService       audit,
        AppDbContext        context)
    {
        _repository = repository;
        _audit      = audit;
        _context    = context;
    }

    public async Task<IEnumerable<LocationDto>> GetAllAsync(string? search = null)
    {
        var items = await _repository.SearchAsync(search);
        return items.Select(x => new LocationDto
        {
            Id              = x.Id,
            Name            = x.Name,
            City            = x.City,
            Country         = x.Country,
            Notes           = x.Notes,
            CreatedAt       = x.CreatedAt,
            DepartmentCount = x.Departments.Count,
            NetworkCount    = x.Networks.Count,
            DeviceCount     = x.Devices.Count
        });
    }

    public async Task<PagedResult<LocationDto>> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        IReadOnlyCollection<int>? allowedLocationIds)
    {
        var query = _context.Locations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();

            query = query.Where(l =>
                l.Name.ToLower().Contains(s) ||
                l.City.ToLower().Contains(s) ||
                l.Country.ToLower().Contains(s));
        }

        if (allowedLocationIds != null)
        {
            query = query.Where(l =>
                allowedLocationIds.Contains(l.Id));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(l => l.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LocationDto
            {
                Id              = l.Id,
                Name            = l.Name,
                City            = l.City,
                Country         = l.Country,
                Notes            = l.Notes,
                CreatedAt       = l.CreatedAt,
                DepartmentCount = l.Departments.Count,
                NetworkCount    = l.Networks.Count,
                DeviceCount     = l.Devices.Count
            })
            .ToListAsync();

        return new PagedResult<LocationDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<LocationDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetDetailsByIdAsync(id);
        if (item == null) return null;

        return new LocationDto
        {
            Id              = item.Id,
            Name            = item.Name,
            City            = item.City,
            Country         = item.Country,
            Notes           = item.Notes,
            CreatedAt       = item.CreatedAt,
            DepartmentCount = item.Departments.Count,
            NetworkCount    = item.Networks.Count,
            DeviceCount     = item.Devices.Count
        };
    }

    public async Task<LocationDetailsDto?> GetDetailsByIdAsync(int id, IReadOnlyCollection<int>? allowedDepartmentIds = null)
    {
        var item = await _repository.GetDetailsByIdAsync(id);
        if (item == null) return null;

        // Isolatiegrens is het departement: op een locatie met meerdere
        // departementen mag iemand met toegang tot slechts één ervan de
        // andere departementen (en hun netwerken/toestellen) niet zien —
        // ook niet via deze locatiepagina.
        var departments = allowedDepartmentIds != null
            ? item.Departments.Where(d => allowedDepartmentIds.Contains(d.Id)).ToList()
            : item.Departments.ToList();

        var visibleDeptIds = departments.Select(d => d.Id).ToHashSet();

        var networks = allowedDepartmentIds != null
            ? item.Networks.Where(n => visibleDeptIds.Contains(n.DepartmentId)).ToList()
            : item.Networks.ToList();

        var devices = allowedDepartmentIds != null
            ? item.Devices.Where(d => visibleDeptIds.Contains(d.DepartmentId)).ToList()
            : item.Devices.ToList();

        return new LocationDetailsDto
        {
            Id        = item.Id,
            Name      = item.Name,
            City      = item.City,
            Country   = item.Country,
            Notes     = item.Notes,
            CreatedAt = item.CreatedAt,
            Departments = departments.Select(d => new DepartmentSummaryDto
            {
                Id           = d.Id,
                Name         = d.Name,
                Address      = d.Address,
                ContactCount = d.Contacts.Count
            }),
            Networks = networks.Select(n => new NetworkSummaryDto
            {
                Id             = n.Id,
                Name           = n.Name,
                NetworkAddress = n.NetworkAddress,
                Cidr           = n.Cidr,
                DeviceCount    = n.Devices.Count
            }),
            Devices = devices.Select(d => new Application.DTOs.Devices.DeviceDto
            {
                Id          = d.Id,
                Name        = d.Name,
                DeviceType  = d.DeviceType,
                Status      = d.Status,
                NetworkId   = d.NetworkId,
                NetworkName = d.Network?.Name
            })
        };
    }

    public async Task CreateAsync(CreateLocationDto dto)
    {
        var entity = new Location
        {
            Name    = dto.Name,
            City    = dto.City,
            Country = dto.Country,
            Notes   = dto.Notes
        };
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        await _audit.LogAsync("CREATE", "Location", entity.Id, entity.Name,
            newValues: new { entity.Name, entity.City, entity.Country, entity.Notes });
    }

    public async Task UpdateAsync(UpdateLocationDto dto)
    {
        var entity = await _repository.GetByIdAsync(dto.Id);
        if (entity == null) return;
        var old = new { entity.Name, entity.City, entity.Country, entity.Notes };
        entity.Name    = dto.Name;
        entity.City    = dto.City;
        entity.Country = dto.Country;
        entity.Notes   = dto.Notes;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
        await _audit.LogAsync("UPDATE", "Location", entity.Id, entity.Name,
            oldValues: old,
            newValues: new { entity.Name, entity.City, entity.Country, entity.Notes });
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return;
        var snapshot = new { entity.Name, entity.City, entity.Country, entity.Notes };
        _repository.Delete(entity);
        await _repository.SaveChangesAsync();
        await _audit.LogAsync("DELETE", "Location", id, snapshot.Name, oldValues: snapshot);
    }
}