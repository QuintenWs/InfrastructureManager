using InfrastructureManager.Application.Common;
using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.Filters;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IDeviceService
{
    Task<IEnumerable<DeviceDto>> GetAllAsync(string? search = null);
    Task<DeviceDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(CreateDeviceDto dto);   // returns new device id
    Task UpdateAsync(UpdateDeviceDto dto);
    Task DeleteAsync(int id);
    Task<IEnumerable<DeviceDto>> FilterAsync(DeviceFilter filter);
    Task<PagedResult<DeviceDto>> FilterPagedAsync(DeviceFilter filter, int page, int pageSize);
}