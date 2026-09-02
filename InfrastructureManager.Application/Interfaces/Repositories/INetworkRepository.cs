using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Application.Filters;

namespace InfrastructureManager.Application.Interfaces.Repositories;

public interface INetworkRepository
    : IGenericRepository<Network>
{
    Task<IEnumerable<Network>> SearchAsync(
        string? search);

    Task<Network?> GetDetailsByIdAsync(
        int id);

    Task<IEnumerable<Network>> FilterAsync(
        NetworkFilter filter);
}