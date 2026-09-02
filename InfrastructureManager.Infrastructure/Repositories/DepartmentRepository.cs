using InfrastructureManager.Application.Interfaces.Repositories;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Repositories;

public class DepartmentRepository : GenericRepository<Department>, IDepartmentRepository
{
    public DepartmentRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Department>> SearchAsync(string? search = null)
    {
        var query = _context.Departments
            .Include(x => x.Location)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.Location.Name.ToLower().Contains(search) ||
                x.Location.City.ToLower().Contains(search));
        }

        return await query.OrderBy(x => x.Location.Name).ThenBy(x => x.Name).ToListAsync();
    }

    public async Task<IEnumerable<Department>> GetByLocationAsync(int locationId)
    {
        return await _context.Departments
            .Include(x => x.Location)
            .Include(x => x.Contacts)
            .Where(x => x.LocationId == locationId)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<Department?> GetDetailsByIdAsync(int id)
    {
        return await _context.Departments
            .Include(x => x.Location)
            .Include(x => x.Contacts)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
