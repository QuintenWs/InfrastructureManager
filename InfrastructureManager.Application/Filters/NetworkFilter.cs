// ── Application/Filters/NetworkFilter.cs ─────────────────────────────────────
namespace InfrastructureManager.Application.Filters;

public class NetworkFilter
{
    public string? Search               { get; set; }
    public bool?   IsDhcpEnabled        { get; set; }
    public bool?   IsInternetAccessible { get; set; }
    public int?    LocationId           { get; set; }
    public int?    DepartmentId         { get; set; }
    public int?    VlanId               { get; set; }
    public string? IspName              { get; set; }
}
