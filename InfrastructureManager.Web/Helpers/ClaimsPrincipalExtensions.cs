using System.Security.Claims;
using InfrastructureManager.Infrastructure.Identity;

namespace InfrastructureManager.Web.Helpers;

/// <summary>
/// Kleine helper om rol-gebaseerde UI-zichtbaarheid consistent te houden
/// tussen alle Views, zodat niet elke pagina zijn eigen variant van
/// "User.IsInRole(...) || User.IsInRole(...)" hoeft te herhalen.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// True voor Admin of Editor. Gebruik dit op lijstpagina's die al
    /// gefilterd zijn op de toegestane departementen van de gebruiker
    /// (Devices/Contacts/Networks/Departments/Visits/InventoryChecks Index)
    /// — daar vallen "zichtbaar" en "bewerkbaar" voor een Editor altijd
    /// samen, omdat IUserAccessService.CanEditDepartmentAsync voor die rol
    /// identiek is aan CanAccessDepartmentAsync.
    ///
    /// Gebruik dit NIET op Details/Edit-pagina's van één specifiek record
    /// (rechtstreeks via een id bereikt) — een Viewer kan daar wél
    /// leestoegang hebben zonder bewerkrecht, dus daar is de exacte,
    /// per-departement CanEditDepartmentAsync-check nodig (zie
    /// DepartmentDetailsViewModel.CanEdit en gelijkaardige velden op de
    /// andere Details-ViewModels).
    /// </summary>
    public static bool IsEditorOrAdmin(this ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.Editor);
}