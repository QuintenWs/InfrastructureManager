using InfrastructureManager.Application.Common;
using InfrastructureManager.Application.DTOs.Locations;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class LocationService : ILocationService
{
    private readonly IAuditService _audit;
    private readonly AppDbContext  _context;

    public LocationService(IAuditService audit, AppDbContext context)
    {
        _audit   = audit;
        _context = context;
    }

    public async Task<IEnumerable<LocationDto>> GetAllAsync(string? search = null)
    {
        var query = _context.Locations
            .Include(x => x.Departments).ThenInclude(d => d.Networks)
            .Include(x => x.Departments).ThenInclude(d => d.Devices)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s) ||
                x.City.ToLower().Contains(s) ||
                x.Country.ToLower().Contains(s));
        }

        var items = await query.OrderBy(x => x.Name).ToListAsync();

        // NB: NetworkCount/DeviceCount telden voorheen altijd 0 in deze
        // methode — de repository laadde Departments nooit met Networks/
        // Devices erbij (enkel Include(x => x.Departments) zonder ThenInclude),
        // terwijl de service wél probeerde te tellen. Onzichtbaar tot nu
        // omdat deze methode nergens gebruikt wordt op een scherm waar die
        // aantallen zichtbaar zijn — nu wel correct dankzij de Includes hierboven.
        return items.Select(x => new LocationDto
        {
            Id              = x.Id,
            Name            = x.Name,
            City            = x.City,
            Country         = x.Country,
            Notes           = x.Notes,
            CreatedAt       = x.CreatedAt,
            DepartmentCount = x.Departments.Count,
            NetworkCount    = x.Departments.Sum(d => d.Networks.Count),
            DeviceCount     = x.Departments.Sum(d => d.Devices.Count)
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
                Notes           = l.Notes,
                CreatedAt       = l.CreatedAt,
                DepartmentCount = l.Departments.Count,
                NetworkCount    = l.Departments.SelectMany(d => d.Networks).Count(),
                DeviceCount     = l.Departments.SelectMany(d => d.Devices).Count()
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
        var item = await _context.Locations
            .Include(x => x.Departments).ThenInclude(d => d.Networks)
            .Include(x => x.Departments).ThenInclude(d => d.Devices)
            .FirstOrDefaultAsync(x => x.Id == id);

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
            NetworkCount    = item.Departments.Sum(d => d.Networks.Count),
            DeviceCount     = item.Departments.Sum(d => d.Devices.Count)
        };
    }

    public async Task<LocationDetailsDto?> GetDetailsByIdAsync(int id, IReadOnlyCollection<int>? allowedDepartmentIds = null)
    {
        var item = await _context.Locations
            .Include(x => x.Departments).ThenInclude(d => d.Contacts)
            .Include(x => x.Departments).ThenInclude(d => d.Networks)
            .Include(x => x.Departments).ThenInclude(d => d.Devices).ThenInclude(dev => dev.Network)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item == null) return null;

        var departments = allowedDepartmentIds != null
            ? item.Departments.Where(d => allowedDepartmentIds.Contains(d.Id)).ToList()
            : item.Departments.ToList();

        var networks = departments.SelectMany(d => d.Networks).ToList();
        var devices  = departments.SelectMany(d => d.Devices).ToList();

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

        _context.Locations.Add(entity);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("CREATE", "Location", entity.Id, entity.Name,
            newValues: new { entity.Name, entity.City, entity.Country, entity.Notes });
    }

    public async Task UpdateAsync(UpdateLocationDto dto)
    {
        var entity = await _context.Locations.FindAsync(dto.Id);
        if (entity == null) return;

        var old = new { entity.Name, entity.City, entity.Country, entity.Notes };

        entity.Name      = dto.Name;
        entity.City      = dto.City;
        entity.Country   = dto.Country;
        entity.Notes     = dto.Notes;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _audit.LogAsync("UPDATE", "Location", entity.Id, entity.Name,
            oldValues: old,
            newValues: new { entity.Name, entity.City, entity.Country, entity.Notes });
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Locations.FindAsync(id);
        if (entity == null) return;
        var snapshot = new { entity.Name, entity.City, entity.Country, entity.Notes };

        // AccessGroupGrant/UserAccessGrant-rijen die toegang geven tot exact
        // déze locatie (via LocationId, niet DepartmentId) worden niet
        // automatisch mee verwijderd door de databank — zie ClientSetNull-
        // comments in AppDbContext.
        await _context.AccessGroupGrants.Where(g => g.LocationId == id).ExecuteDeleteAsync();
        await _context.UserAccessGrants.Where(g => g.LocationId == id).ExecuteDeleteAsync();

        _context.Locations.Remove(entity);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("DELETE", "Location", id, snapshot.Name, oldValues: snapshot);
    }
}