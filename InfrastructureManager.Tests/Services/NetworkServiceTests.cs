using InfrastructureManager.Application.DTOs.Networks;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Domain.Exceptions;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Services;
using InfrastructureManager.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InfrastructureManager.Tests.Services;

public class NetworkServiceTests
{
    private static NetworkService CreateSut(out AppDbContext context)
    {
        context = InMemoryDbContextFactory.Create();
        var auditMock = new Mock<IAuditService>();
        return new NetworkService(auditMock.Object, context);
    }

    [Fact]
    public async Task CreateAsync_OverlappingSubnet_ThrowsSubnetValidationException()
    {
        var sut = CreateSut(out var context);
        var department = await TestData.AddDepartmentAsync(context);
        context.ChangeTracker.Clear();

        await sut.CreateAsync(new CreateNetworkDto
        {
            DepartmentId = department.Id, Name = "LAN A",
            NetworkAddress = "192.168.1.0", SubnetMask = "255.255.255.0", Cidr = 24,
            Gateway = "192.168.1.1", PrimaryDns = "8.8.8.8"
        });

        // 192.168.1.128/25 valt volledig binnen 192.168.1.0/24 hierboven.
        await Assert.ThrowsAsync<SubnetValidationException>(() => sut.CreateAsync(new CreateNetworkDto
        {
            DepartmentId = department.Id, Name = "LAN B",
            NetworkAddress = "192.168.1.128", SubnetMask = "255.255.255.128", Cidr = 25,
            Gateway = "192.168.1.129", PrimaryDns = "8.8.8.8"
        }));
    }

    [Fact]
    public async Task CreateAsync_NonOverlappingSubnet_Succeeds()
    {
        var sut = CreateSut(out var context);
        var department = await TestData.AddDepartmentAsync(context);
        context.ChangeTracker.Clear();

        await sut.CreateAsync(new CreateNetworkDto
        {
            DepartmentId = department.Id, Name = "LAN A",
            NetworkAddress = "192.168.1.0", SubnetMask = "255.255.255.0", Cidr = 24,
            Gateway = "192.168.1.1", PrimaryDns = "8.8.8.8"
        });
        await sut.CreateAsync(new CreateNetworkDto
        {
            DepartmentId = department.Id, Name = "LAN B",
            NetworkAddress = "192.168.2.0", SubnetMask = "255.255.255.0", Cidr = 24,
            Gateway = "192.168.2.1", PrimaryDns = "8.8.8.8"
        });

        Assert.Equal(2, await context.Networks.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_InvalidNetworkAddressForCidr_ThrowsSubnetValidationException()
    {
        var sut = CreateSut(out var context);
        var department = await TestData.AddDepartmentAsync(context);
        context.ChangeTracker.Clear();

        // 192.168.1.5 is geen geldig netwerkadres voor /24 (host-bits gezet).
        await Assert.ThrowsAsync<SubnetValidationException>(() => sut.CreateAsync(new CreateNetworkDto
        {
            DepartmentId = department.Id, Name = "LAN A",
            NetworkAddress = "192.168.1.5", SubnetMask = "255.255.255.0", Cidr = 24,
            Gateway = "192.168.1.1", PrimaryDns = "8.8.8.8"
        }));
    }

    [Fact]
    public async Task FilterPagedAsync_RespectsAllowedDepartmentIds()
    {
        // Beveiligingsregressie: als AllowedDepartmentIds ooit per ongeluk niet
        // meer toegepast wordt, faalt dit onmiddellijk.
        var sut = CreateSut(out var context);

        var location = await TestData.AddLocationAsync(context);
        var deptA    = await TestData.AddDepartmentAsync(context, "Dept A", location);
        var deptB    = await TestData.AddDepartmentAsync(context, "Dept B", location);
        await TestData.AddNetworkAsync(context, deptA.Id, "LAN A", "10.0.0.0", "255.255.255.0", 24, "10.0.0.1");
        await TestData.AddNetworkAsync(context, deptB.Id, "LAN B", "10.0.1.0", "255.255.255.0", 24, "10.0.1.1");
        context.ChangeTracker.Clear();

        var result = await sut.FilterPagedAsync(
            new NetworkFilter { AllowedDepartmentIds = new[] { deptA.Id } }, page: 1, pageSize: 20);

        Assert.Single(result.Items);
        Assert.Equal("LAN A", result.Items[0].Name);
    }

    [Fact]
    public async Task SuggestNextFreeIpAsync_FullSubnet_ReturnsNull()
    {
        var sut = CreateSut(out var context);

        var department = await TestData.AddDepartmentAsync(context);
        // /30 = slechts 2 bruikbare host-adressen: .1 en .2
        var network    = await TestData.AddNetworkAsync(context, department.Id, "LAN", "10.0.0.0", "255.255.255.252", 30, "10.0.0.1");
        var definition = await TestData.AddDefinitionAsync(context, DeviceType.Switch, "Switch");
        var field      = await TestData.AddFieldAsync(context, definition.Id, "IP Address", "ip_address");
        var device     = await TestData.AddDeviceAsync(context, department.Id, DeviceType.Switch, network.Id, "SW-01");

        // .1 is bezet door de gateway zelf, .2 door dit device
        await TestData.AddFieldValueAsync(context, device.Id, field.Id, "10.0.0.2");
        context.ChangeTracker.Clear();

        var suggestion = await sut.SuggestNextFreeIpAsync(network.Id);

        Assert.Null(suggestion);
    }

    [Fact]
    public async Task SuggestNextFreeIpAsync_SkipsGatewayAndOccupiedAddresses()
    {
        var sut = CreateSut(out var context);

        var department = await TestData.AddDepartmentAsync(context);
        // /29 = 6 bruikbare host-adressen: .1 t.e.m. .6
        var network    = await TestData.AddNetworkAsync(context, department.Id, "LAN", "10.0.0.0", "255.255.255.248", 29, "10.0.0.1");
        var definition = await TestData.AddDefinitionAsync(context, DeviceType.Switch, "Switch");
        var field      = await TestData.AddFieldAsync(context, definition.Id, "IP Address", "ip_address");
        var device     = await TestData.AddDeviceAsync(context, department.Id, DeviceType.Switch, network.Id, "SW-01");
        await TestData.AddFieldValueAsync(context, device.Id, field.Id, "10.0.0.2");
        context.ChangeTracker.Clear();

        var suggestion = await sut.SuggestNextFreeIpAsync(network.Id);

        // .1 = gateway (bezet), .2 = device (bezet), dus de eerste vrije is .3
        Assert.Equal("10.0.0.3", suggestion);
    }
}