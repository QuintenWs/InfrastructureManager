using InfrastructureManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InfrastructureManager.Web.ViewModels.Devices;

public class DeviceFilterViewModel
{
    public string?       Search       { get; set; }
    public DeviceType?   DeviceType   { get; set; }
    public DeviceStatus? Status       { get; set; }
    public int?          LocationId   { get; set; }
    public int?          DepartmentId { get; set; }

    public IEnumerable<SelectListItem> Locations { get; set; }
        = new List<SelectListItem>();
}