using InfrastructureManager.Application.DTOs.InventoryChecks;
using InfrastructureManager.Application.Common;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IInventoryCheckService
{
    Task<IEnumerable<InventoryCheckSummaryDto>> GetByDepartmentAsync(int departmentId);

    Task<IEnumerable<InventoryCheckSummaryDto>> GetRecentAsync(int take = 10);

    Task<InventoryCheckDetailDto?> GetByIdAsync(int id);

    Task<int> CreateAsync(CreateInventoryCheckDto dto);

    Task<(byte[] Data, string ContentType, string FileName)?> GetPhotoAsync(int itemId);

    Task<PagedResult<InventoryCheckSummaryDto>> GetByDepartmentPagedAsync(int departmentId, int page, int pageSize);
    
    Task<PagedResult<InventoryCheckSummaryDto>> GetRecentPagedAsync(int page, int pageSize);
}
