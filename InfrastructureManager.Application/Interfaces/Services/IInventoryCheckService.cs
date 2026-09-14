using InfrastructureManager.Application.DTOs.InventoryChecks;
using InfrastructureManager.Application.Common;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IInventoryCheckService
{
    Task<IEnumerable<InventoryCheckSummaryDto>> GetByDepartmentAsync(int departmentId);

    Task<IEnumerable<InventoryCheckSummaryDto>> GetRecentAsync(int take = 10, IReadOnlyCollection<int>? allowedDepartmentIds = null);

    Task<InventoryCheckDetailDto?> GetByIdAsync(int id);

    Task<int> CreateAsync(CreateInventoryCheckDto dto);

    Task<(byte[] Data, string ContentType, string FileName, int DepartmentId)?> GetPhotoAsync(int itemId);

    /// <summary>Departement van een controle-item — gebruikt om leestoegang te controleren op het Photo-eindpunt.</summary>
    Task<int?> GetDepartmentIdForItemAsync(int itemId);

    Task<PagedResult<InventoryCheckSummaryDto>> GetByDepartmentPagedAsync(int departmentId, int page, int pageSize);

    Task<PagedResult<InventoryCheckSummaryDto>> GetRecentPagedAsync(int page, int pageSize, IReadOnlyCollection<int>? allowedDepartmentIds = null);
}