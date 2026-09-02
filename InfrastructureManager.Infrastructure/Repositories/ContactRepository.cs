using InfrastructureManager.Application.Interfaces.Repositories;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Repositories;

public class ContactRepository : GenericRepository<Contact>, IContactRepository
{
    public ContactRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Contact>> GetAllWithDetailsAsync(string? search = null)
    {
        var query = _context.Contacts
            .Include(x => x.Department)
                .ThenInclude(d => d.Location)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(x =>
                x.FirstName.ToLower().Contains(search)    ||
                x.LastName.ToLower().Contains(search)     ||
                x.Email.ToLower().Contains(search)        ||
                (x.Role != null && x.Role.ToLower().Contains(search)) ||
                x.Department.Name.ToLower().Contains(search)          ||
                x.Department.Location.Name.ToLower().Contains(search));
        }

        return await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToListAsync();
    }

    public async Task<Contact?> GetDetailsByIdAsync(int id)
    {
        return await _context.Contacts
            .Include(x => x.Department)
                .ThenInclude(d => d.Location)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<Contact>> GetByDepartmentAsync(int departmentId)
    {
        return await _context.Contacts
            .Include(x => x.Department)
                .ThenInclude(d => d.Location)
            .Where(x => x.DepartmentId == departmentId)
            .OrderBy(x => x.LastName)
            .ToListAsync();
    }
}