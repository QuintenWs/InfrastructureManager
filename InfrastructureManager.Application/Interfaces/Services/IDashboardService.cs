using InfrastructureManager.Application.DTOs.Dashboard;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(
        int? locationId = null,
        IReadOnlyCollection<int>? allowedLocationIds = null,
        int recentDevicesCount = 5,
        int recentActivityCount = 10);
}
