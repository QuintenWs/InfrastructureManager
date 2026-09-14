namespace InfrastructureManager.Application.Interfaces.Services;

public interface IMaintenanceLogService
{
    Task<IEnumerable<MaintenanceLogDto>> GetByDeviceAsync(int deviceId);
    Task AddAsync(int deviceId, string note);
    Task DeleteAsync(int logId);

    /// <summary>Departement van het toestel waarbij deze notitie hoort —
    /// gebruikt om schrijftoegang te controleren zonder de (mogelijk
    /// vervalste) deviceId-parameter van de client te vertrouwen.</summary>
    Task<int?> GetDepartmentIdForLogAsync(int logId);
}

public class MaintenanceLogDto
{
    public int      Id              { get; set; }
    public int      DeviceId        { get; set; }
    public string   UserDisplayName { get; set; } = string.Empty;
    public string   Note            { get; set; } = string.Empty;
    public DateTime CreatedAt       { get; set; }
}