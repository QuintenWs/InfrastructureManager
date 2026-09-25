namespace InfrastructureManager.Web.ViewModels.Devices;

public class DeviceIndexViewModel
{
    public IEnumerable<DeviceListViewModel> Devices { get; set; }
        = new List<DeviceListViewModel>();

    public DeviceFilterViewModel Filter { get; set; }
        = new();

    public InfrastructureManager.Web.ViewModels.Shared.PaginationViewModel Pagination { get; set; } = new();
}