using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Repositories;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Repositories;

public class DeviceRepository : GenericRepository<Device>, IDeviceRepository
{
    public DeviceRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Device>> SearchAsync(string? search = null)
    {
        var query = _context.Devices
            .Include(x => x.Department)
            .Include(x => x.Location)
            .Include(x => x.Network)
            .Include(x => x.FieldValues).ThenInclude(v => v.Field)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s) ||
                x.Department.Name.ToLower().Contains(s) ||
                x.Location.Name.ToLower().Contains(s) ||
                // Also search in field values
                x.FieldValues.Any(v => v.Value.ToLower().Contains(s)));
        }

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<IEnumerable<Device>> FilterAsync(DeviceFilter filter)
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

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<Device?> GetDetailsByIdAsync(int id)
    {
        return await _context.Devices
            .Include(x => x.Department).ThenInclude(d => d.Location)
            .Include(x => x.Network)
            .Include(x => x.FieldValues).ThenInclude(v => v.Field)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}