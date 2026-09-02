namespace InfrastructureManager.Application.Interfaces.Services;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        int entityId,
        string entityLabel,
        object? oldValues = null,
        object? newValues = null);
}
