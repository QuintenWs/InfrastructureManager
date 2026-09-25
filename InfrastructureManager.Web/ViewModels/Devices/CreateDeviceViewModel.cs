using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace InfrastructureManager.Web.ViewModels.Devices;

public class CreateDeviceViewModel
{
    [Required(ErrorMessage = "Department is required.")]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Display(Name = "Network")]
    public int? NetworkId { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(200)]
    [Display(Name = "Device Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Device Type")]
    public int DeviceType { get; set; }

    [Display(Name = "Status")]
    public DeviceStatus Status { get; set; } = DeviceStatus.Active;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public List<DeviceTypeFieldDto> TypeFields  { get; set; } = new();
    public Dictionary<int, string>  FieldValues { get; set; } = new();

    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Networks    { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> DeviceTypes { get; set; } = new List<SelectListItem>();
}