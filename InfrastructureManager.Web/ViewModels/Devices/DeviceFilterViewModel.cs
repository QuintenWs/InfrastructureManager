using Microsoft.AspNetCore.Mvc.Rendering;
using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Web.ViewModels.Devices;

public class DeviceFilterViewModel
{
    public string?       Search       { get; set; }
    public int?          DeviceType   { get; set; }
    public DeviceStatus? Status       { get; set; }
    public int?          LocationId   { get; set; }
    public int?          DepartmentId { get; set; }

    public IEnumerable<SelectListItem> Locations   { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> DeviceTypes { get; set; } = new List<SelectListItem>();
}