using System.Security.Claims;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IUserAccessService
{
    /// <summary>Locaties waartoe deze gebruiker toegang heeft. Null = geen beperking (alles zichtbaar).</summary>
    Task<List<int>?> GetAccessibleLocationIdsAsync(ClaimsPrincipal user);

    Task<bool> CanAccessLocationAsync(ClaimsPrincipal user, int locationId);
    Task<bool> CanAccessDepartmentAsync(ClaimsPrincipal user, int departmentId);

    /// <summary>Voor het admin-scherm: welke locaties zijn nu toegewezen.</summary>
    Task<List<int>> GetAssignedLocationIdsAsync(string userId);

    /// <summary>Vervangt de volledige set toegewezen locaties voor deze gebruiker.</summary>
    Task SetAssignedLocationsAsync(string userId, IEnumerable<int> locationIds);
}