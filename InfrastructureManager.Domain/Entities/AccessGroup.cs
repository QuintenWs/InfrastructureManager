namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// Een team/groep gebruikers die dezelfde toegangsrechten delen — bv.
/// "IT Operations Antwerpen". Vervangt het aanvinken van locaties per
/// individuele gebruiker: een groep wordt één keer geconfigureerd en
/// gebruikers worden er simpelweg aan toegevoegd/verwijderd.
/// </summary>
public class AccessGroup
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Als true, mogen alle leden van deze groep de audit-geschiedenis
    /// (History) bekijken — beperkt tot de departementen die ze al mogen
    /// zien. Los van de individuele ApplicationUser.CanViewHistory-vlag.
    /// </summary>
    public bool CanViewHistory { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AccessGroupGrant> Grants { get; set; } = new List<AccessGroupGrant>();

    public ICollection<UserAccessGroup> Members { get; set; } = new List<UserAccessGroup>();
}