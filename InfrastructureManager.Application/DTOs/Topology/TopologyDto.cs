namespace InfrastructureManager.Application.DTOs.Topology;

public class TopologyDto
{
    public int    DepartmentId   { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string LocationName   { get; set; } = string.Empty;

    public IEnumerable<TopologyNetworkNode> Networks          { get; set; } = new List<TopologyNetworkNode>();
    public IEnumerable<TopologyDeviceNode>  UnassignedDevices { get; set; } = new List<TopologyDeviceNode>();

    /// <summary>Saved node positions from DB. Null = use auto layout.</summary>
    public Dictionary<string, NodePosition>? SavedPositions { get; set; }

    /// <summary>Custom edges — overrides the default device→network connections.</summary>
    public IEnumerable<CustomEdge> CustomEdges { get; set; } = new List<CustomEdge>();
}

public class TopologyNetworkNode
{
    public int    Id                   { get; set; }
    public string Name                 { get; set; } = string.Empty;
    public string NetworkAddress       { get; set; } = string.Empty;
    public int    Cidr                 { get; set; }
    public string Gateway              { get; set; } = string.Empty;
    public bool   IsInternetAccessible { get; set; }

    public IEnumerable<TopologyDeviceNode> Devices { get; set; } = new List<TopologyDeviceNode>();
}

public class TopologyDeviceNode
{
    public int    Id         { get; set; }
    public string Name       { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Status     { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}

public class NodePosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class CustomEdge
{
    public string From { get; set; } = string.Empty; // "dev_5" or "net_2" or "cloud"
    public string To   { get; set; } = string.Empty;
}
