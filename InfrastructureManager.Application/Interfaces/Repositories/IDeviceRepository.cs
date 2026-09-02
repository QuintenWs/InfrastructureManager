using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Application.Filters;

namespace InfrastructureManager.Application.Interfaces.Repositories;

public interface IDeviceRepository
    : IGenericRepository<Device>
{
    Task<IEnumerable<Device>> SearchAsync(
        string? search);

    Task<Device?> GetDetailsByIdAsync(int id);

    Task<IEnumerable<Device>> FilterAsync(
    DeviceFilter filter);
}