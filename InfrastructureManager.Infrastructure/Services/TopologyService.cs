using InfrastructureManager.Application.DTOs.Topology;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InfrastructureManager.Infrastructure.Services;

public class TopologyService : ITopologyService
{
    private readonly AppDbContext _context;

    public TopologyService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TopologyDto?> GetByDepartmentAsync(int departmentId)
    {
        var dept = await _context.Departments
            .Include(d => d.Location)
            .Include(d => d.Networks)
            .Include(d => d.Devices)
                .ThenInclude(dev => dev.FieldValues)
                    .ThenInclude(fv => fv.Field)
            .FirstOrDefaultAsync(d => d.Id == departmentId);

        if (dept == null) return null;

        var layout = await _context.TopologyLayouts
            .FirstOrDefaultAsync(l => l.DepartmentId == departmentId);

        Dictionary<string, NodePosition>? savedPositions = null;
        var customEdges = new List<CustomEdge>();

        if (layout != null)
        {
            if (!string.IsNullOrWhiteSpace(layout.NodePositions))
                try { savedPositions = JsonSerializer.Deserialize<Dictionary<string, NodePosition>>(
                    layout.NodePositions, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
                catch { }

            if (!string.IsNullOrWhiteSpace(layout.CustomEdges))
                try { customEdges = JsonSerializer.Deserialize<List<CustomEdge>>(
                    layout.CustomEdges, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new(); }
                catch { }
        }

        var networks = dept.Networks.OrderBy(n => n.Name).Select(n => new TopologyNetworkNode
        {
            Id = n.Id, Name = n.Name, NetworkAddress = n.NetworkAddress,
            Cidr = n.Cidr, Gateway = n.Gateway, IsInternetAccessible = n.IsInternetAccessible,
            Devices = dept.Devices.Where(d => d.NetworkId == n.Id).OrderBy(d => d.Name).Select(MapDevice)
        });

        var unassigned = dept.Devices.Where(d => d.NetworkId == null).OrderBy(d => d.Name).Select(MapDevice);

        return new TopologyDto
        {
            DepartmentId = dept.Id, DepartmentName = dept.Name, LocationName = dept.Location.Name,
            Networks = networks, UnassignedDevices = unassigned,
            SavedPositions = savedPositions, CustomEdges = customEdges
        };
    }

    public async Task SaveLayoutAsync(int departmentId,
        Dictionary<string, NodePosition> positions, List<CustomEdge> edges)
    {
        var layout = await _context.TopologyLayouts
            .FirstOrDefaultAsync(l => l.DepartmentId == departmentId);

        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        if (layout == null)
            _context.TopologyLayouts.Add(new Domain.Entities.TopologyLayout
            {
                DepartmentId  = departmentId,
                NodePositions = JsonSerializer.Serialize(positions, opts),
                CustomEdges   = JsonSerializer.Serialize(edges, opts),
                UpdatedAt     = DateTime.UtcNow
            });
        else
        {
            layout.NodePositions = JsonSerializer.Serialize(positions, opts);
            layout.CustomEdges   = JsonSerializer.Serialize(edges, opts);
            layout.UpdatedAt     = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task ResetLayoutAsync(int departmentId)
    {
        var layout = await _context.TopologyLayouts
            .FirstOrDefaultAsync(l => l.DepartmentId == departmentId);
        if (layout != null)
        {
            _context.TopologyLayouts.Remove(layout);
            await _context.SaveChangesAsync();
        }
    }

    private static TopologyDeviceNode MapDevice(Domain.Entities.Device d) => new()
    {
        Id = d.Id, Name = d.Name, DeviceType = d.DeviceType.ToString(), Status = d.Status.ToString(),
        IpAddress = d.FieldValues.FirstOrDefault(fv =>
            fv.Field?.FieldType == "ipv4" || fv.Field?.FieldType == "ipv6" ||
            fv.Field?.FieldKey == "ip_address")?.Value
    };
}
