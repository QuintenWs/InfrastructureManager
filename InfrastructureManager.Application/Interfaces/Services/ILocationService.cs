using InfrastructureManager.Application.Common;
using InfrastructureManager.Application.DTOs.Locations;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface ILocationService
{
    Task<IEnumerable<LocationDto>> GetAllAsync(string? search = null);
    Task<PagedResult<LocationDto>> GetPagedAsync(string? search, int page, int pageSize);
    Task<LocationDto?> GetByIdAsync(int id);
    Task<LocationDetailsDto?> GetDetailsByIdAsync(int id);
    Task CreateAsync(CreateLocationDto dto);
    Task UpdateAsync(UpdateLocationDto dto);
    Task DeleteAsync(int id);
    Task<PagedResult<LocationDto>> GetPagedAsync(string? search, int page, int pageSize, IReadOnlyCollection<int>? allowedLocationIds = null);
}