namespace InfrastructureManager.Domain.Entities;

public class Location : BaseEntity
{
    public string Name    { get; set; } = string.Empty;
    public string City    { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Notes  { get; set; }

    public ICollection<Department> Departments { get; set; } = new List<Department>();
    // Photos removed — photos now belong to Department
}
