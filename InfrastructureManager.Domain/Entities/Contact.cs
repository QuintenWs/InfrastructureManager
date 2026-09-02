namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// A responsible person for a department.
/// Location is implicit via Department → Location.
/// </summary>
public class Contact : BaseEntity
{
    public int DepartmentId { get; set; }

    public Department Department { get; set; } = null!;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    /// <summary>e.g. "IT Manager", "Network Admin", "Site Responsible"</summary>
    public string? Role { get; set; }

    public string? Notes { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
