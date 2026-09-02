namespace InfrastructureManager.Web.ViewModels.Networks;

public class NetworkIndexViewModel
{
    public IEnumerable<NetworkListViewModel>
        Networks { get; set; }
            = new List<NetworkListViewModel>();

    public NetworkFilterViewModel Filter { get; set; }
        = new();
}