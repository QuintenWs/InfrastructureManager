using InfrastructureManager.Domain.Entities;

namespace InfrastructureManager.Application.Interfaces.Repositories;

public interface IDepartmentRepository : IGenericRepository<Department>
{
    Task<IEnumerable<Department>> SearchAsync(string? search = null);
    Task<IEnumerable<Department>> GetByLocationAsync(int locationId);
    Task<Department?> GetDetailsByIdAsync(int id);
}
