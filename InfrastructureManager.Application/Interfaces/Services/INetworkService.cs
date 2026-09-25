using InfrastructureManager.Application.DTOs.Networks;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Common;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface INetworkService
{
    Task<NetworkDto?> GetByIdAsync(int id);

    Task CreateAsync(CreateNetworkDto dto);

    Task UpdateAsync(UpdateNetworkDto dto);

    Task DeleteAsync(int id);

    Task<IEnumerable<NetworkDto>> FilterAsync(
    NetworkFilter filter);

    Task<PagedResult<NetworkDto>> FilterPagedAsync(NetworkFilter filter, int page, int pageSize);

    /// <summary>Eerstvolgende vrije host-IP binnen het subnet, of null als het
    /// subnet vol zit of te groot is om automatisch te doorzoeken (> /20).
    /// Houdt enkel rekening met IP-adressen die effectief in de inventaris
    /// geregistreerd staan (zelfde bron als de IP-conflictcontrole) en met het
    /// Gateway-adres van het netwerk zelf.</summary>
    Task<string?> SuggestNextFreeIpAsync(int networkId);
}