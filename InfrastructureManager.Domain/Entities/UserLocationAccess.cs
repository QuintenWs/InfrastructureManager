namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// Beperkt een gebruiker (meestal Viewer) tot één of meerdere locaties/diensten.
/// Geen rijen voor een gebruiker = geen beperking (ziet alles).
/// </summary>
public class UserLocationAccess
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;
}