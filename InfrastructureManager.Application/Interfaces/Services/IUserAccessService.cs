using System.Security.Claims;
using InfrastructureManager.Application.DTOs.AccessGroups;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IUserAccessService
{
    // ── Effectieve leesscope (gebruikt overal waar data wordt opgehaald) ────

    /// <summary>Departementen waartoe deze gebruiker toegang heeft. Null = geen
    /// beperking (Admin — ziet alles). Een lege lijst betekent letterlijk
    /// "ziet niets" — dit is een geldige, fail-closed uitkomst.</summary>
    Task<List<int>?> GetAccessibleDepartmentIdsAsync(ClaimsPrincipal user);

    /// <summary>Locaties die minstens één toegankelijk departement bevatten.
    /// Null = geen beperking.</summary>
    Task<List<int>?> GetAccessibleLocationIdsAsync(ClaimsPrincipal user);

    Task<bool> CanAccessDepartmentAsync(ClaimsPrincipal user, int departmentId);
    Task<bool> CanAccessLocationAsync(ClaimsPrincipal user, int locationId);

    // ── Schrijfscope (Editor-rol) ────────────────────────────────────────────

    /// <summary>True voor Admin (altijd), of voor Editor mits hij dit
    /// departement ook mag zien. Viewer geeft altijd false, ongeacht scope.</summary>
    Task<bool> CanEditDepartmentAsync(ClaimsPrincipal user, int departmentId);

    // ── Geschiedenis (History) ───────────────────────────────────────────────

    /// <summary>True voor Admin (altijd), of als de gebruiker zelf of een van
    /// zijn groepen CanViewHistory heeft.</summary>
    Task<bool> CanViewHistoryAsync(ClaimsPrincipal user);

    // ── Admin-scherm helpers: resolutie voor een willekeurige gebruiker ─────
    // (geen ClaimsPrincipal beschikbaar wanneer een Admin een ándere
    // gebruiker beheert)

    Task<bool> IsUnrestrictedAsync(string userId);
    Task<List<int>> GetAccessibleDepartmentIdsForUserAsync(string userId);

    /// <summary>Aantal effectief toegankelijke departementen — voor de
    /// overzichtskolom op het Users-scherm.</summary>
    Task<int> GetAccessibleDepartmentCountAsync(string userId);

    Task<bool> GetCanViewHistoryForUserAsync(string userId);
    Task SetCanViewHistoryForUserAsync(string userId, bool canViewHistory);

    // ── Groepenbeheer (volledig dynamisch, door Admin te bedienen) ──────────

    Task<List<AccessGroupSummaryDto>> GetAllAccessGroupsAsync();
    Task<AccessGroupDetailsDto?> GetAccessGroupDetailsAsync(int id);
    Task<int> CreateAccessGroupAsync(CreateAccessGroupDto dto);
    Task UpdateAccessGroupAsync(UpdateAccessGroupDto dto);
    Task DeleteAccessGroupAsync(int id);

    Task AddDepartmentGrantToGroupAsync(int accessGroupId, int departmentId);
    Task AddLocationGrantToGroupAsync(int accessGroupId, int locationId);
    Task RemoveGroupGrantAsync(int grantId);

    // ── Groepslidmaatschap ────────────────────────────────────────────────────

    Task<List<int>> GetGroupIdsForUserAsync(string userId);

    /// <summary>Vervangt de volledige set groepen waarin deze gebruiker zit.</summary>
    Task SetUserGroupsAsync(string userId, IEnumerable<int> accessGroupIds);

    /// <summary>Voegt de gebruiker toe aan één groep, zonder zijn overige
    /// lidmaatschappen te wijzigen — gebruikt vanaf het groepsscherm zelf.</summary>
    Task AddUserToGroupAsync(string userId, int accessGroupId);

    /// <summary>Verwijdert de gebruiker uit één groep, zonder zijn overige
    /// lidmaatschappen te wijzigen.</summary>
    Task RemoveUserFromGroupAsync(string userId, int accessGroupId);

    // ── Individuele uitzonderingen (los van groepen) ─────────────────────────

    Task<List<AccessGrantDto>> GetIndividualGrantsForUserAsync(string userId);
    Task AddDepartmentGrantToUserAsync(string userId, int departmentId);
    Task AddLocationGrantToUserAsync(string userId, int locationId);
    Task RemoveIndividualGrantAsync(int grantId);
}