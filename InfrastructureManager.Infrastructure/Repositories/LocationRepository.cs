using InfrastructureManager.Application.Interfaces.Repositories;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Repositories;

public class LocationRepository : GenericRepository<Location>, ILocationRepository
{
    public LocationRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Location>> SearchAsync(string? search = null)
    {
        var query = _context.Locations
            .Include(x => x.Departments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.City.ToLower().Contains(search) ||
                x.Country.ToLower().Contains(search));
        }

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<Location?> GetDetailsByIdAsync(int id)
    {
        return await _context.Locations
            .Include(x => x.Departments)
                .ThenInclude(d => d.Contacts)
            .Include(x => x.Networks)
            .Include(x => x.Devices)
                .ThenInclude(d => d.Network)
            // Photos removed — photos now belong to Department
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}