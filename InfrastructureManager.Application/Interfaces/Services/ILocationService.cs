using InfrastructureManager.Application.Common;
using InfrastructureManager.Application.DTOs.Locations;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface ILocationService
{
    Task<IEnumerable<LocationDto>> GetAllAsync(string? search = null);
    Task<LocationDto?> GetByIdAsync(int id);

    /// <summary>allowedDepartmentIds filtert de Departments/Networks/Devices
    /// sub-lijsten binnen deze locatie — anders zou iemand met toegang tot
    /// slechts één departement op deze locatie ook de andere departementen
    /// (en hun netwerken/toestellen) van dezelfde locatie te zien krijgen.</summary>
    Task<LocationDetailsDto?> GetDetailsByIdAsync(int id, IReadOnlyCollection<int>? allowedDepartmentIds = null);

    Task CreateAsync(CreateLocationDto dto);
    Task UpdateAsync(UpdateLocationDto dto);
    Task DeleteAsync(int id);
    Task<PagedResult<LocationDto>> GetPagedAsync(string? search, int page, int pageSize, IReadOnlyCollection<int>? allowedLocationIds = null);
}