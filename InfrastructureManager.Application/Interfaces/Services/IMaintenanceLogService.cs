namespace InfrastructureManager.Application.Interfaces.Services;

public interface IMaintenanceLogService
{
    Task<IEnumerable<MaintenanceLogDto>> GetByDeviceAsync(int deviceId);
    Task AddAsync(int deviceId, string note);
    Task DeleteAsync(int logId);
}

public class MaintenanceLogDto
{
    public int      Id              { get; set; }
    public int      DeviceId        { get; set; }
    public string   UserDisplayName { get; set; } = string.Empty;
    public string   Note            { get; set; } = string.Empty;
    public DateTime CreatedAt       { get; set; }
}
