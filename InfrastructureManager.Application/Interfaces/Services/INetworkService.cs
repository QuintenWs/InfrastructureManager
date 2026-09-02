using InfrastructureManager.Application.DTOs.Networks;
using InfrastructureManager.Application.Filters;


namespace InfrastructureManager.Application.Interfaces.Services;

public interface INetworkService
{
    Task<IEnumerable<NetworkDto>> GetAllAsync(
        string? search = null);

    Task<NetworkDto?> GetByIdAsync(int id);

    Task CreateAsync(CreateNetworkDto dto);

    Task UpdateAsync(UpdateNetworkDto dto);

    Task DeleteAsync(int id);

    Task<IEnumerable<NetworkDto>> FilterAsync(
    NetworkFilter filter);
}