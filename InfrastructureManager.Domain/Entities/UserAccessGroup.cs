namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// Lidmaatschap van een gebruiker in een AccessGroup (many-to-many — een
/// gebruiker kan in meerdere groepen tegelijk zitten).
/// </summary>
public class UserAccessGroup
{
    public int Id { get; set; }

    /// <summary>ASP.NET Identity user ID. Geen navigatie-property naar
    /// ApplicationUser, want Domain mag niet naar Infrastructure/Identity
    /// verwijzen — de FK-relatie zelf wordt wel in AppDbContext geconfigureerd.</summary>
    public string UserId { get; set; } = string.Empty;

    public int AccessGroupId { get; set; }
    public AccessGroup AccessGroup { get; set; } = null!;
}