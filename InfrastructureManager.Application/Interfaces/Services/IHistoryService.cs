using InfrastructureManager.Application.DTOs.History;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IHistoryService
{
    Task<HistoryPageResult> SearchAsync(HistoryFilter filter, IReadOnlyCollection<int>? allowedDepartmentIds = null);

    /// <summary>Distinct entity types that actually appear in the log, for the type filter dropdown.</summary>
    Task<IEnumerable<string>> GetEntityTypesAsync(IReadOnlyCollection<int>? allowedDepartmentIds = null);

    /// <summary>Distinct users that appear in the log, for the user filter dropdown.</summary>
    Task<IEnumerable<(string UserId, string DisplayName)>> GetUsersAsync(IReadOnlyCollection<int>? allowedDepartmentIds = null);
}