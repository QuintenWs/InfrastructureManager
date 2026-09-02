namespace InfrastructureManager.Web.ViewModels.Devices;

public class DeviceIndexViewModel
{
    public IEnumerable<DeviceListViewModel> Devices
        = new List<DeviceListViewModel>();

    public DeviceFilterViewModel Filter
        = new();

    public InfrastructureManager.Web.ViewModels.Shared.PaginationViewModel Pagination { get; set; } = new();
}