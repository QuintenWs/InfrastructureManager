using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Repositories;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Repositories;

public class NetworkRepository : GenericRepository<Network>, INetworkRepository
{
    public NetworkRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Network>> SearchAsync(string? search = null)
    {
        var query = _context.Networks
            .Include(x => x.Department)
                .ThenInclude(d => d.Location)
            .Include(x => x.Devices)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query  = query.Where(x =>
                x.Name.ToLower().Contains(search)           ||
                x.NetworkAddress.ToLower().Contains(search) ||
                x.Department.Name.ToLower().Contains(search)||
                x.Department.Location.Name.ToLower().Contains(search));
        }

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<Network?> GetDetailsByIdAsync(int id)
    {
        return await _context.Networks
            .Include(x => x.Department)
                .ThenInclude(d => d.Location)
            .Include(x => x.Devices)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<Network>> FilterAsync(NetworkFilter filter)
    {
        var query = _context.Networks
            .Include(x => x.Department)
                .ThenInclude(d => d.Location)
            .Include(x => x.Devices)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s)           ||
                x.NetworkAddress.ToLower().Contains(s) ||
                x.Gateway.ToLower().Contains(s)        ||
                x.Department.Name.ToLower().Contains(s)||
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
            query = query.Where(x => x.IspName != null &&
                                    x.IspName.Contains(filter.IspName));

        return await query.OrderBy(x => x.Name).ToListAsync();
    }
}
