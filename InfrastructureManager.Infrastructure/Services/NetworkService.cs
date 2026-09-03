using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.DTOs.Networks;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Repositories;
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
    private readonly INetworkRepository _networkRepository;
    private readonly IAuditService      _audit;
    private readonly AppDbContext       _context;

    public NetworkService(
        INetworkRepository networkRepository,
        IAuditService      audit,
        AppDbContext       context)
    {
        _networkRepository = networkRepository;
        _audit             = audit;
        _context           = context;
    }

    public async Task<IEnumerable<NetworkDto>> GetAllAsync(string? search = null)
    {
        var items = await _networkRepository.SearchAsync(search);
        return items.Select(ToDto);
    }

    public async Task<PagedResult<NetworkDto>> FilterPagedAsync(NetworkFilter filter, int page, int pageSize)
    {
        var query = _context.Networks
            .Include(x => x.Department).ThenInclude(d => d.Location)
            .Include(x => x.Devices)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s)            ||
                x.NetworkAddress.ToLower().Contains(s)  ||
                x.Gateway.ToLower().Contains(s)         ||
                x.Department.Name.ToLower().Contains(s) ||
                (x.IspName != null && x.IspName.ToLower().Contains(s)));
        }

        if (filter.IsDhcpEnabled.HasValue)
            query = query.Where(x => x.IsDhcpEnabled == filter.IsDhcpEnabled.Value);

        if (filter.IsInternetAccessible.HasValue)
            query = query.Where(x => x.IsInternetAccessible == filter.IsInternetAccessible.Value);

        if (filter.DepartmentId.HasValue)
            query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);

        if (filter.LocationId.HasValue)
            query = query.Where(x => x.LocationId == filter.LocationId.Value);

        if (filter.VlanId.HasValue)
            query = query.Where(x => x.VlanId == filter.VlanId.Value);

        if (!string.IsNullOrWhiteSpace(filter.IspName))
            query = query.Where(x => x.IspName != null && x.IspName.Contains(filter.IspName));
        
        if (filter.AllowedLocationIds != null)
            query = query.Where(x => filter.AllowedLocationIds.Contains(x.LocationId));

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
        var x = await _networkRepository.GetDetailsByIdAsync(id);
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
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == dto.DepartmentId)
            ?? throw new ArgumentException($"Department {dto.DepartmentId} not found.");

        ValidateSubnet(dto.NetworkAddress, dto.Cidr);
        await CheckOverlapAsync(dto.NetworkAddress, dto.Cidr, dto.DepartmentId, null);

        var entity = new Network
        {
            DepartmentId         = dto.DepartmentId,
            LocationId           = department.LocationId,
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

        await _networkRepository.AddAsync(entity);
        await _networkRepository.SaveChangesAsync();

        await _audit.LogAsync("CREATE", "Network", entity.Id, entity.Name,
            newValues: new
            {
                entity.Name, entity.NetworkAddress, entity.Cidr, entity.SubnetMask, entity.Gateway,
                entity.PrimaryDns, entity.SecondaryDns, entity.DhcpRangeStart, entity.DhcpRangeEnd,
                entity.IsDhcpEnabled, entity.IsInternetAccessible, entity.VlanId, entity.IspName,
                entity.Notes, entity.DepartmentId
            });
    }

    public async Task UpdateAsync(UpdateNetworkDto dto)
    {
        var entity = await _networkRepository.GetByIdAsync(dto.Id);
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

        if (entity.DepartmentId != dto.DepartmentId)
        {
            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == dto.DepartmentId);
            if (dept != null) entity.LocationId = dept.LocationId;
        }

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

        _networkRepository.Update(entity);
        await _networkRepository.SaveChangesAsync();

        await _audit.LogAsync("UPDATE", "Network", entity.Id, entity.Name,
            oldValues: old,
            newValues: new
            {
                entity.Name, entity.NetworkAddress, entity.Cidr, entity.SubnetMask, entity.Gateway,
                entity.PrimaryDns, entity.SecondaryDns, entity.DhcpRangeStart, entity.DhcpRangeEnd,
                entity.IsDhcpEnabled, entity.IsInternetAccessible, entity.VlanId, entity.IspName,
                entity.Notes, entity.DepartmentId
            });
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _networkRepository.GetByIdAsync(id);
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

        _networkRepository.Delete(entity);
        await _networkRepository.SaveChangesAsync();

        await _audit.LogAsync("DELETE", "Network", id, snapshot.Name, oldValues: snapshot);
    }

    public async Task<IEnumerable<NetworkDto>> FilterAsync(NetworkFilter filter)
    {
        var items = await _networkRepository.FilterAsync(filter);
        return items.Select(ToDto);
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

    private static NetworkDto ToDto(Network x) => new()
    {
        Id                   = x.Id,
        DepartmentId         = x.DepartmentId,
        DepartmentName       = x.Department.Name,
        LocationId           = x.LocationId,
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
