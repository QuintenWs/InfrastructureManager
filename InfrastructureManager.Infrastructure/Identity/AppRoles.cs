namespace InfrastructureManager.Infrastructure.Identity;

/// <summary>
/// Centralised role name constants — use these instead of magic strings
/// so a rename only needs to happen in one place.
/// </summary>
public static class AppRoles
{
    /// <summary>Onbeperkt: ziet en beheert alles, beheert ook groepen/rechten van anderen.</summary>
    public const string Admin  = "Admin";

    /// <summary>
    /// Gescoped exact zoals Viewer (ziet enkel zijn toegestane departementen),
    /// maar mag binnen die scope wél aanmaken/wijzigen/verwijderen —
    /// bv. het hoofd van een departement.
    /// </summary>
    public const string Editor = "Editor";

    /// <summary>Gescoped, enkel lezen — geen enkele schrijfactie toegestaan.</summary>
    public const string Viewer = "Viewer";

    /// <summary>Voor [Authorize(Roles = ...)] op schrijfacties: laat zowel Admin
    /// als Editor door de rolcontrole — de effectieve departement-scope wordt
    /// daarna nog apart gecontroleerd via IUserAccessService.CanEditDepartmentAsync.</summary>
    public const string AdminOrEditor = Admin + "," + Editor;
}