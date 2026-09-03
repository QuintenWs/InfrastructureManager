namespace InfrastructureManager.Application.DTOs.Dashboard;

public class OverdueVisitDto
{
    public int       DepartmentId       { get; set; }
    public string    DepartmentName     { get; set; } = string.Empty;
    public string    LocationName       { get; set; } = string.Empty;
    public DateTime? LastVisitDate      { get; set; }   // null = nog nooit bezocht
    public int       DaysSinceLastVisit { get; set; }
}