using InfrastructureManager.Domain.Entities;

namespace InfrastructureManager.Application.Interfaces.Repositories;

public interface IContactRepository : IGenericRepository<Contact>
{
    Task<IEnumerable<Contact>> GetAllWithDetailsAsync(string? search = null);
    Task<Contact?> GetDetailsByIdAsync(int id);
    Task<IEnumerable<Contact>> GetByDepartmentAsync(int departmentId);
}
