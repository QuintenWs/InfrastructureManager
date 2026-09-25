using InfrastructureManager.Application.DTOs.Topology;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InfrastructureManager.Web.ViewModels.Topology;

public class TopologyIndexViewModel
{
    public IEnumerable<SelectListItem> Departments  { get; set; } = new List<SelectListItem>();
    public int?         DepartmentId { get; set; }
    public bool          CanEdit      { get; set; }
    public TopologyDto?  Topology     { get; set; }
}