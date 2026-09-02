namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// Stores the saved layout (node positions + custom edges) for a department topology.
/// One row per department. If no row exists, the automatic layout is used.
/// </summary>
public class TopologyLayout
{
    public int    Id           { get; set; }
    public int    DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    /// <summary>JSON: { "net_1": {x, y}, "dev_5": {x, y}, "cloud": {x, y} }</summary>
    public string? NodePositions { get; set; }

    /// <summary>JSON: [ {from: "dev_5", to: "net_2"}, ... ] — manual edge overrides</summary>
    public string? CustomEdges   { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
