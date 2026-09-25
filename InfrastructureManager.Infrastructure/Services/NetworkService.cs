using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.DTOs.Networks;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Domain.Exceptions;
using InfrastructureManager.Domain.Helpers;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using InfrastructureManager.Application.Common;

namespace InfrastructureManager.Infrastructure.Services;

public class NetworkService : INetworkService
{
    private readonly IAuditService _audit;
    private readonly AppDbContext  _context;

    public NetworkService(IAuditService audit, AppDbContext context)
    {
        _audit   = audit;
        _context = context;
    }

    public async Task<PagedResult<NetworkDto>> FilterPagedAsync(NetworkFilter filter, int page, int pageSize)
    {
        var query = BuildQuery(search: filter.Search, filter: filter);

        var totalCount = await query.CountAsync();

        var entities = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<NetworkDto>
        {
            Items      = entities.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<NetworkDto?> GetByIdAsync(int id)
    {
        var x = await _context.Networks
            .Include(n => n.Department).ThenInclude(d => d.Location)
            .Include(n => n.Devices)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (x == null) return null;

        var dto = ToDto(x);
        dto.Devices = x.Devices.Select(d => new DeviceDto
        {
            Id         = d.Id,
            Name       = d.Name,
            DeviceType = d.DeviceType,
            Status     = d.Status
        });
        return dto;
    }

    public async Task CreateAsync(CreateNetworkDto dto)
    {
        var departmentExists = await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId);
        if (!departmentExists)
            throw new ArgumentException($"Department {dto.DepartmentId} not found.");

        ValidateSubnet(dto.NetworkAddress, dto.Cidr);
        await CheckOverlapAsync(dto.NetworkAddress, dto.Cidr, dto.DepartmentId, null);

        var entity = new Network
        {
            DepartmentId         = dto.DepartmentId,
            Name                 = dto.Name,
            NetworkAddress       = dto.NetworkAddress,
            SubnetMask           = dto.SubnetMask,
            Cidr                 = dto.Cidr,
            Gateway              = dto.Gateway,
            PrimaryDns           = dto.PrimaryDns,
            SecondaryDns         = dto.SecondaryDns   ?? string.Empty,
            DhcpRangeStart       = dto.DhcpRangeStart ?? string.Empty,
            DhcpRangeEnd         = dto.DhcpRangeEnd   ?? string.Empty,
            IsDhcpEnabled        = dto.IsDhcpEnabled,
            IsInternetAccessible = dto.IsInternetAccessible,
            VlanId               = dto.VlanId,
            IspName              = dto.IspName,
            Notes                = dto.Notes
        };

        _context.Networks.Add(entity);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("CREATE", "Network", entity.Id, entity.Name,
            newValues: new
            {
                entity.Name, entity.NetworkAddress, entity.Cidr, entity.SubnetMask, entity.Gateway,
                entity.PrimaryDns, entity.SecondaryDns, entity.DhcpRangeStart, entity.DhcpRangeEnd,
                entity.IsDhcpEnabled, entity.IsInternetAccessible, entity.VlanId, entity.IspName,
                entity.Notes, entity.DepartmentId
            },
            departmentId: entity.DepartmentId);
    }

    public async Task UpdateAsync(UpdateNetworkDto dto)
    {
        var entity = await _context.Networks.FindAsync(dto.Id);
        if (entity == null) return;

        var old = new
        {
            entity.Name, entity.NetworkAddress, entity.Cidr, entity.SubnetMask, entity.Gateway,
            entity.PrimaryDns, entity.SecondaryDns, entity.DhcpRangeStart, entity.DhcpRangeEnd,
            entity.IsDhcpEnabled, entity.IsInternetAccessible, entity.VlanId, entity.IspName,
            entity.Notes, entity.DepartmentId
        };

        ValidateSubnet(dto.NetworkAddress, dto.Cidr);
        await CheckOverlapAsync(dto.NetworkAddress, dto.Cidr, dto.DepartmentId, dto.Id);

        entity.DepartmentId         = dto.DepartmentId;
        entity.Name                 = dto.Name;
        entity.NetworkAddress       = dto.NetworkAddress;
        entity.SubnetMask           = dto.SubnetMask;
        entity.Cidr                 = dto.Cidr;
        entity.Gateway              = dto.Gateway;
        entity.PrimaryDns           = dto.PrimaryDns;
        entity.SecondaryDns         = dto.SecondaryDns   ?? string.Empty;
        entity.DhcpRangeStart       = dto.DhcpRangeStart ?? string.Empty;
        entity.DhcpRangeEnd         = dto.DhcpRangeEnd   ?? string.Empty;
        entity.IsDhcpEnabled        = dto.IsDhcpEnabled;
        entity.IsInternetAccessible = dto.IsInternetAccessible;
        entity.VlanId               = dto.VlanId;
        entity.IspName              = dto.IspName;
        entity.Notes                = dto.Notes;
        entity.UpdatedAt            = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _audit.LogAsync("UPDATE", "Network", entity.Id, entity.Name,
            oldValues: old,
            newValues: new
            {
                entity.Name, entity.NetworkAddress, entity.Cidr, entity.SubnetMask, entity.Gateway,
                entity.PrimaryDns, entity.SecondaryDns, entity.DhcpRangeStart, entity.DhcpRangeEnd,
                entity.IsDhcpEnabled, entity.IsInternetAccessible, entity.VlanId, entity.IspName,
                entity.Notes, entity.DepartmentId
            },
            departmentId: entity.DepartmentId);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Networks.FindAsync(id);
        if (entity == null) return;

        var snapshot = new
        {
            entity.Name, entity.NetworkAddress, entity.Cidr, entity.SubnetMask, entity.Gateway,
            entity.PrimaryDns, entity.SecondaryDns, entity.IsDhcpEnabled, entity.IsInternetAccessible,
            entity.VlanId, entity.IspName, entity.Notes, entity.DepartmentId
        };

        await _context.Devices
            .Where(d => d.NetworkId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.NetworkId, (int?)null));

        _context.Networks.Remove(entity);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("DELETE", "Network", id, snapshot.Name, oldValues: snapshot, departmentId: snapshot.DepartmentId);
    }

    public async Task<IEnumerable<NetworkDto>> FilterAsync(NetworkFilter filter)
    {
        var query = BuildQuery(search: filter.Search, filter: filter);
        var items = await query.OrderBy(x => x.Name).ToListAsync();
        return items.Select(ToDto);
    }

    /// <summary>
    /// Eén gedeelde queryopbouw voor GetAllAsync/FilterAsync/FilterPagedAsync,
    /// zodat een filterveld nooit nog in slechts één van de drie paden
    /// toegepast kan worden (was voorheen apart onderhouden in repository
    /// vs. service).
    /// </summary>
    private IQueryable<Network> BuildQuery(string? search, NetworkFilter? filter)
    {
        var query = _context.Networks
            .Include(x => x.Department).ThenInclude(d => d.Location)
            .Include(x => x.Devices)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s)            ||
                x.NetworkAddress.ToLower().Contains(s)  ||
                x.Gateway.ToLower().Contains(s)          ||
                x.Department.Name.ToLower().Contains(s)  ||
                (x.IspName != null && x.IspName.ToLower().Contains(s)));
        }

        if (filter == null) return query;

        if (filter.IsDhcpEnabled.HasValue)
            query = query.Where(x => x.IsDhcpEnabled == filter.IsDhcpEnabled.Value);

        if (filter.IsInternetAccessible.HasValue)
            query = query.Where(x => x.IsInternetAccessible == filter.IsInternetAccessible.Value);

        if (filter.DepartmentId.HasValue)
            query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);

        if (filter.LocationId.HasValue)
            query = query.Where(x => x.Department.LocationId == filter.LocationId.Value);

        if (filter.VlanId.HasValue)
            query = query.Where(x => x.VlanId == filter.VlanId.Value);

        if (!string.IsNullOrWhiteSpace(filter.IspName))
            query = query.Where(x => x.IspName != null && x.IspName.Contains(filter.IspName));

        if (filter.AllowedDepartmentIds != null)
            query = query.Where(x => filter.AllowedDepartmentIds.Contains(x.DepartmentId));

        return query;
    }

    private static void ValidateSubnet(string networkAddress, int cidr)
    {
        if (!SubnetHelper.IsValidNetworkAddress(networkAddress, cidr))
        {
            var correct = SubnetHelper.GetNetworkAddress(networkAddress, cidr);
            throw new SubnetValidationException(
                $"'{networkAddress}' is not a valid network address for /{cidr}. " +
                $"Did you mean '{correct}'?");
        }
    }

    private async Task CheckOverlapAsync(string networkAddress, int cidr, int departmentId, int? excludeId)
    {
        var existing = await _context.Networks
            .Where(n => n.DepartmentId == departmentId)
            .Where(n => excludeId == null || n.Id != excludeId.Value)
            .Select(n => new { n.Name, n.NetworkAddress, n.Cidr })
            .ToListAsync();

        foreach (var other in existing)
        {
            if (SubnetHelper.Overlaps(networkAddress, cidr, other.NetworkAddress, other.Cidr))
                throw new SubnetValidationException(
                    $"Network {networkAddress}/{cidr} overlaps with '{other.Name}' ({other.NetworkAddress}/{other.Cidr}).");
        }
    }

    public async Task<string?> SuggestNextFreeIpAsync(int networkId)
    {
        var network = await _context.Networks.FindAsync(networkId);
        if (network == null) return null;

        var hostBits = 32 - network.Cidr;
        if (hostBits <= 1 || hostBits > 12) return null; // geen host-bereik, of subnet te groot (> 4094 adressen)

        var occupied = await _context.DeviceFieldValues
            .Where(v => v.Device.NetworkId == networkId &&
                        (v.Field.FieldType == "ipv4" || v.Field.FieldType == "ipv6" || v.Field.FieldKey == "ip_address"))
            .Select(v => v.Value)
            .ToListAsync();

        var occupiedSet = new HashSet<string>(occupied, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(network.Gateway))
            occupiedSet.Add(network.Gateway);

        foreach (var candidate in SubnetHelper.GetHostAddresses(network.NetworkAddress, network.Cidr))
        {
            if (!occupiedSet.Contains(candidate))
                return candidate;
        }

        return null; 
    }

    private static NetworkDto ToDto(Network x) => new()
    {
        Id                   = x.Id,
        DepartmentId         = x.DepartmentId,
        DepartmentName       = x.Department.Name,
        LocationId           = x.Department.LocationId,
        LocationName         = x.Department.Location.Name,
        Name                 = x.Name,
        NetworkAddress       = x.NetworkAddress,
        SubnetMask           = x.SubnetMask,
        Cidr                 = x.Cidr,
        Gateway              = x.Gateway,
        PrimaryDns           = x.PrimaryDns,
        SecondaryDns         = x.SecondaryDns,
        DhcpRangeStart       = x.DhcpRangeStart,
        DhcpRangeEnd         = x.DhcpRangeEnd,
        IsDhcpEnabled        = x.IsDhcpEnabled,
        IsInternetAccessible = x.IsInternetAccessible,
        VlanId               = x.VlanId,
        IspName              = x.IspName,
        Notes                = x.Notes,
        DeviceCount          = x.Devices.Count
    };
}