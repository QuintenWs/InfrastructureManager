using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Web.ViewModels.Devices;

public class DeviceDetailsViewModel
{
    public int    Id             { get; set; }
    public int    DepartmentId   { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string LocationName   { get; set; } = string.Empty;
    public string? NetworkName   { get; set; }
    public string Name           { get; set; } = string.Empty;
    public DeviceType   DeviceType { get; set; }
    public DeviceStatus Status     { get; set; }
    public string? Notes { get; set; }
    public IEnumerable<DeviceDocumentDto> Documents { get; set; } = new List<DeviceDocumentDto>();

    /// <summary>Only fields that have a value.</summary>
    public IReadOnlyList<DeviceTypeFieldDto> TypeFields { get; set; }
        = new List<DeviceTypeFieldDto>();

    public IEnumerable<MaintenanceLogDto> MaintenanceLogs { get; set; }
        = new List<MaintenanceLogDto>();
}