namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// Individuele uitzondering, rechtstreeks aan één gebruiker toegekend, los
/// van elke groep — bv. "deze ene persoon mag ook heel Antwerpen zien".
/// Exact één van DepartmentId/LocationId staat ingevuld (zelfde principe
/// als AccessGroupGrant).
/// </summary>
public class UserAccessGrant
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int? LocationId { get; set; }
    public Location? Location { get; set; }
}