using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace InfrastructureManager.Web.ViewModels.Networks;

public class UpdateNetworkViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Department is required.")]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(200)]
    [Display(Name = "Network Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Network address is required.")]
    [RegularExpression(@"^(\d{1,3}\.){3}\d{1,3}$",
        ErrorMessage = "Enter a valid IP address (e.g. 192.168.1.0).")]
    [Display(Name = "Network Address")]
    public string NetworkAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subnet mask is required.")]
    [RegularExpression(@"^(\d{1,3}\.){3}\d{1,3}$",
        ErrorMessage = "Enter a valid subnet mask.")]
    [Display(Name = "Subnet Mask")]
    public string SubnetMask { get; set; } = string.Empty;

    [Required(ErrorMessage = "CIDR is required.")]
    [Range(0, 32, ErrorMessage = "CIDR must be between 0 and 32.")]
    [Display(Name = "CIDR")]
    public int Cidr { get; set; }

    [Required(ErrorMessage = "Gateway is required.")]
    [RegularExpression(@"^(\d{1,3}\.){3}\d{1,3}$",
        ErrorMessage = "Enter a valid gateway IP address.")]
    [Display(Name = "Gateway")]
    public string Gateway { get; set; } = string.Empty;

    [Required(ErrorMessage = "Primary DNS is required.")]
    [RegularExpression(@"^(\d{1,3}\.){3}\d{1,3}$",
        ErrorMessage = "Enter a valid DNS IP address.")]
    [Display(Name = "Primary DNS")]
    public string PrimaryDns { get; set; } = string.Empty;

    [Display(Name = "Secondary DNS")]
    public string? SecondaryDns { get; set; }

    [Display(Name = "DHCP Enabled")]
    public bool IsDhcpEnabled { get; set; }

    [Display(Name = "DHCP Range Start")]
    public string? DhcpRangeStart { get; set; }

    [Display(Name = "DHCP Range End")]
    public string? DhcpRangeEnd { get; set; }

    [Display(Name = "Internet Accessible")]
    public bool IsInternetAccessible { get; set; }

    [Range(1, 4094, ErrorMessage = "VLAN ID must be between 1 and 4094.")]
    [Display(Name = "VLAN ID")]
    public int? VlanId { get; set; }

    [MaxLength(100)]
    [Display(Name = "ISP Name")]
    public string? IspName { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
}