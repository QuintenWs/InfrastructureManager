using InfrastructureManager.Application.DTOs.InventoryChecks;
using InfrastructureManager.Application.Common;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IInventoryCheckService
{
    Task<InventoryCheckDetailDto?> GetByIdAsync(int id);

    Task<int> CreateAsync(CreateInventoryCheckDto dto);

    Task<(byte[] Data, string ContentType, string FileName, int DepartmentId)?> GetPhotoAsync(int itemId);

    /// <summary>Departement van een controle-item — gebruikt om leestoegang te controleren op het Photo-eindpunt.</summary>
    Task<int?> GetDepartmentIdForItemAsync(int itemId);

    Task<PagedResult<InventoryCheckSummaryDto>> GetByDepartmentPagedAsync(int departmentId, int page, int pageSize);

    Task<PagedResult<InventoryCheckSummaryDto>> GetRecentPagedAsync(int page, int pageSize, IReadOnlyCollection<int>? allowedDepartmentIds = null);

    /// <summary>Datum van de meest recente controle voor een departement, of
    /// null als er nog geen is — vermijdt dat alle controles (met al hun Items)
    /// geladen moeten worden enkel om de eerste te pakken.</summary>
    Task<DateTime?> GetLastCheckDateAsync(int departmentId);

    /// <summary>Deletes a registered check. Admin-only from the controller.
    /// InventoryCheckItem cascades automatically at the DB level (true Cascade,
    /// not ClientSetNull) — no manual cleanup needed, unlike the SiteVisit
    /// case above.</summary>
    Task DeleteAsync(int id);
}