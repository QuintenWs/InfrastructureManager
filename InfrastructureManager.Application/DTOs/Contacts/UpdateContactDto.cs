namespace InfrastructureManager.Application.DTOs.Contacts;

public class UpdateContactDto
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Role { get; set; }
    public string? Notes { get; set; }
}