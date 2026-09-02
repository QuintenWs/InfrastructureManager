namespace InfrastructureManager.Application.DTOs.Departments;

public class CreateDepartmentDto
{
    public int LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Notes { get; set; }
}