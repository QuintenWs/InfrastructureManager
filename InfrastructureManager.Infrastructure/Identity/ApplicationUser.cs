using Microsoft.AspNetCore.Identity;

namespace InfrastructureManager.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Individuele uitzondering: als true, mag deze gebruiker de
    /// audit-geschiedenis (History) bekijken voor de departementen die hij
    /// al mag zien — ongeacht of een van zijn groepen dat recht ook geeft.
    /// Admins hebben dit recht altijd, ongeacht deze vlag.
    /// </summary>
    public bool CanViewHistory { get; set; } = false;
}