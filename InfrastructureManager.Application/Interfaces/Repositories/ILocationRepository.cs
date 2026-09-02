using InfrastructureManager.Domain.Entities;

namespace InfrastructureManager.Application.Interfaces.Repositories;

public interface ILocationRepository : IGenericRepository<Location>
{
    Task<IEnumerable<Location>> SearchAsync(string? search = null);
    Task<Location?> GetDetailsByIdAsync(int id);
}
