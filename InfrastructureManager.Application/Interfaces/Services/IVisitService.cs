using InfrastructureManager.Application.DTOs.Visits;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IVisitService
{
    /// <summary>Visit history for one department, most recent first.</summary>
    Task<IEnumerable<SiteVisitDto>> GetVisitsByDepartmentAsync(int departmentId);

    /// <summary>A single visit including the items resolved/created during it.</summary>
    Task<SiteVisitDto?> GetVisitByIdAsync(int id);

    /// <summary>Open + in-progress items for one department, highest priority / oldest first.</summary>
    Task<IEnumerable<ActionItemDto>> GetOpenActionItemsByDepartmentAsync(int departmentId);

    /// <summary>Open + in-progress items across all departments (optionally scoped to a location).</summary>
    Task<IEnumerable<ActionItemDto>> GetAllOpenActionItemsAsync(int? locationId = null);

    Task<int> GetOpenActionItemCountAsync(int? departmentId = null);

    /// <summary>Open + in-progress item counts for every department that has any — for list overviews.</summary>
    Task<Dictionary<int, int>> GetOpenActionItemCountsAsync();

    /// <summary>Registers a visit: resolves the selected open items and adds the new ones. Returns the new visit's id.</summary>
    Task<int> CreateVisitAsync(CreateSiteVisitDto dto);

    /// <summary>Marks a single item as "in behandeling" without a full visit.</summary>
    Task SetInProgressAsync(int actionItemId);
}
