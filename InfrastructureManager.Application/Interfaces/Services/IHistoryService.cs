using InfrastructureManager.Application.DTOs.History;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IHistoryService
{
    Task<HistoryPageResult> SearchAsync(HistoryFilter filter);

    /// <summary>Distinct entity types that actually appear in the log, for the type filter dropdown.</summary>
    Task<IEnumerable<string>> GetEntityTypesAsync();

    /// <summary>Distinct users that appear in the log, for the user filter dropdown.</summary>
    Task<IEnumerable<(string UserId, string DisplayName)>> GetUsersAsync();
}
