// ── Web/ViewModels/Networks/NetworkFilterViewModel.cs ─────────────────────────
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InfrastructureManager.Web.ViewModels.Networks;

public class NetworkFilterViewModel
{
    public string? Search               { get; set; }
    public bool?   IsDhcpEnabled        { get; set; }
    public bool?   IsInternetAccessible { get; set; }
    public int?    DepartmentId         { get; set; }
    public int?    VlanId               { get; set; }
    public string? IspName              { get; set; }

    public IEnumerable<SelectListItem> Departments { get; set; }
        = new List<SelectListItem>();
}
