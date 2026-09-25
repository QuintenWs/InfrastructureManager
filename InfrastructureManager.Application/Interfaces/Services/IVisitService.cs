using InfrastructureManager.Application.DTOs.Visits;
using InfrastructureManager.Application.Common;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IVisitService
{
    /// <summary>A single visit including the items resolved/created during it.</summary>
    Task<SiteVisitDto?> GetVisitByIdAsync(int id);

    /// <summary>Open + in-progress items for one department, highest priority / oldest first.</summary>
    Task<IEnumerable<ActionItemDto>> GetOpenActionItemsByDepartmentAsync(int departmentId);

    Task<int> GetOpenActionItemCountAsync(int? departmentId = null);

    /// <summary>Open + in-progress item counts for every department that has any — for list overviews.</summary>
    Task<Dictionary<int, int>> GetOpenActionItemCountsAsync();

    /// <summary>Registers a visit: resolves the selected open items and adds the new ones. Returns the new visit's id.</summary>
    Task<int> CreateVisitAsync(CreateSiteVisitDto dto);

    /// <summary>Marks a single item as "in behandeling" without a full visit.</summary>
    Task SetInProgressAsync(int actionItemId);

    /// <summary>Departement van een actiepunt — gebruikt om schrijftoegang te controleren voor SetInProgress.</summary>
    Task<int?> GetActionItemDepartmentIdAsync(int actionItemId);

    Task<PagedResult<ActionItemDto>> GetAllOpenActionItemsPagedAsync(int page, int pageSize, int? locationId = null, IReadOnlyCollection<int>? allowedDepartmentIds = null);

    Task<PagedResult<SiteVisitDto>>  GetVisitsByDepartmentPagedAsync(int departmentId, int page, int pageSize);

    /// <summary>Verwijdert een geregistreerd bezoek. Admin-only vanuit de
    /// controller. Ontkoppelt eerst de ActionItems die naar dit bezoek
    /// verwijzen (CreatedDuringVisitId/ResolvedDuringVisitId staan op
    /// ClientSetNull, zie AppDbContext) — zonder die stap zou dit dezelfde
    /// DbUpdateException geven als de DeviceTypeDefinition-bug uit stap 1.3.</summary>
    Task DeleteVisitAsync(int id);
}