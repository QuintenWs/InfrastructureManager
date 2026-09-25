using InfrastructureManager.Application.DTOs.Contacts;
using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.DTOs.Networks;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IExportService
{
    byte[] ExportDevices(IEnumerable<DeviceDto> devices);
    byte[] ExportNetworks(IEnumerable<NetworkDto> networks);
    byte[] ExportContacts(IEnumerable<ContactDto> contacts);
}