using InfrastructureManager.Application.DTOs.Topology;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface ITopologyService
{
    Task<TopologyDto?> GetByDepartmentAsync(int departmentId);

    Task SaveLayoutAsync(
        int departmentId,
        Dictionary<string, NodePosition> positions,
        List<CustomEdge> edges);

    Task ResetLayoutAsync(int departmentId);
}