using ClosedXML.Excel;
using InfrastructureManager.Application.DTOs.Contacts;
using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.DTOs.Networks;
using InfrastructureManager.Application.Interfaces.Services;

namespace InfrastructureManager.Infrastructure.Services;

public class ExportService : IExportService
{
    public byte[] ExportDevices(IEnumerable<DeviceDto> devices)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Devices");

        var headers = new[] { "Name", "Department", "Location", "Network", "Type", "Status", "IP Address", "Notes" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var d in devices)
        {
            ws.Cell(row, 1).Value = d.Name;
            ws.Cell(row, 2).Value = d.DepartmentName;
            ws.Cell(row, 3).Value = d.LocationName;
            ws.Cell(row, 4).Value = d.NetworkName ?? "";
            ws.Cell(row, 5).Value = d.DeviceType.ToString();
            ws.Cell(row, 6).Value = d.Status.ToString();
            ws.Cell(row, 7).Value = d.IpAddress ?? "";
            ws.Cell(row, 8).Value = d.Notes ?? "";
            row++;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportNetworks(IEnumerable<NetworkDto> networks)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Networks");

        var headers = new[] { "Name", "Department", "Location", "Network Address", "CIDR", "Gateway", "Primary DNS", "DHCP", "Internet", "VLAN", "ISP", "Devices" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var n in networks)
        {
            ws.Cell(row, 1).Value  = n.Name;
            ws.Cell(row, 2).Value  = n.DepartmentName;
            ws.Cell(row, 3).Value  = n.LocationName;
            ws.Cell(row, 4).Value  = n.NetworkAddress;
            ws.Cell(row, 5).Value  = n.Cidr;
            ws.Cell(row, 6).Value  = n.Gateway;
            ws.Cell(row, 7).Value  = n.PrimaryDns;
            ws.Cell(row, 8).Value  = n.IsDhcpEnabled ? "Yes" : "No";
            ws.Cell(row, 9).Value  = n.IsInternetAccessible ? "Yes" : "No";
            ws.Cell(row, 10).Value = n.VlanId?.ToString() ?? "";
            ws.Cell(row, 11).Value = n.IspName ?? "";
            ws.Cell(row, 12).Value = n.DeviceCount;
            row++;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportContacts(IEnumerable<ContactDto> contacts)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Contacts");

        var headers = new[] { "Full Name", "Role", "Department", "Location", "Email", "Phone", "Notes" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var c in contacts)
        {
            ws.Cell(row, 1).Value = c.FullName;
            ws.Cell(row, 2).Value = c.Role ?? "";
            ws.Cell(row, 3).Value = c.DepartmentName;
            ws.Cell(row, 4).Value = c.LocationName;
            ws.Cell(row, 5).Value = c.Email;
            ws.Cell(row, 6).Value = c.Phone ?? "";
            ws.Cell(row, 7).Value = c.Notes ?? "";
            row++;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}