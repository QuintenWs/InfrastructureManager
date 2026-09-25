using InfrastructureManager.Application.DTOs.Networks;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
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

        var department = new Department { Name = "Dept", Address = "Addr" };
        context.Add(department);
        await context.SaveChangesAsync();

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

        var department = new Department { Name = "Dept", Address = "Addr" };
        context.Add(department);
        await context.SaveChangesAsync();

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

        var department = new Department { Name = "Dept", Address = "Addr" };
        context.Add(department);
        await context.SaveChangesAsync();

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
        // Beveiligingsregressie: als AllowedDepartmentIds ooit per ongeluk
        // niet meer toegepast wordt, zou dit onmiddellijk falen.
        var sut = CreateSut(out var context);

        var deptA = new Department { Name = "Dept A", Address = "Addr" };
        var deptB = new Department { Name = "Dept B", Address = "Addr" };
        context.AddRange(deptA, deptB);
        await context.SaveChangesAsync();

        context.AddRange(
            new Network { DepartmentId = deptA.Id, Name = "LAN A", NetworkAddress = "10.0.0.0", SubnetMask = "255.255.255.0", Cidr = 24, Gateway = "10.0.0.1", PrimaryDns = "8.8.8.8" },
            new Network { DepartmentId = deptB.Id, Name = "LAN B", NetworkAddress = "10.0.1.0", SubnetMask = "255.255.255.0", Cidr = 24, Gateway = "10.0.1.1", PrimaryDns = "8.8.8.8" });
        await context.SaveChangesAsync();

        var result = await sut.FilterPagedAsync(new NetworkFilter { AllowedDepartmentIds = new[] { deptA.Id } }, page: 1, pageSize: 20);

        Assert.Single(result.Items);
        Assert.Equal("LAN A", result.Items[0].Name);
    }

    [Fact]
    public async Task SuggestNextFreeIpAsync_FullSubnet_ReturnsNull()
    {
        var sut = CreateSut(out var context);

        var department = new Department { Name = "Dept", Address = "Addr" };
        var network = new Network
        {
            Department = department, Name = "LAN", NetworkAddress = "10.0.0.0",
            SubnetMask = "255.255.255.252", Cidr = 30, // slechts 2 bruikbare host-adressen: .1 en .2
            Gateway = "10.0.0.1", PrimaryDns = "8.8.8.8"
        };
        context.AddRange(department, network);
        await context.SaveChangesAsync();

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Switch, Name = "Switch" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "IP Address", FieldKey = "ip_address", FieldType = "text" };
        var device     = new Device { Department = department, Network = network, Name = "SW-01", DeviceType = DeviceType.Switch, Status = DeviceStatus.Active };
        context.AddRange(definition, field, device);
        await context.SaveChangesAsync();

        // .1 is al bezet door de Gateway zelf, .2 wordt hier vastgelegd via het device
        context.Add(new DeviceFieldValue { DeviceTypeFieldId = field.Id, DeviceId = device.Id, Value = "10.0.0.2" });
        await context.SaveChangesAsync();

        var suggestion = await sut.SuggestNextFreeIpAsync(network.Id);

        Assert.Null(suggestion);
    }

    [Fact]
    public async Task SuggestNextFreeIpAsync_SkipsGatewayAndOccupiedAddresses()
    {
        var sut = CreateSut(out var context);

        var department = new Department { Name = "Dept", Address = "Addr" };
        var network = new Network
        {
            Department = department, Name = "LAN", NetworkAddress = "10.0.0.0",
            SubnetMask = "255.255.255.248", Cidr = 29, // 6 bruikbare host-adressen: .1 t.e.m. .6
            Gateway = "10.0.0.1", PrimaryDns = "8.8.8.8"
        };
        context.AddRange(department, network);
        await context.SaveChangesAsync();

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Switch, Name = "Switch" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "IP Address", FieldKey = "ip_address", FieldType = "text" };
        var device     = new Device { Department = department, Network = network, Name = "SW-01", DeviceType = DeviceType.Switch, Status = DeviceStatus.Active };
        context.AddRange(definition, field, device);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceTypeFieldId = field.Id, DeviceId = device.Id, Value = "10.0.0.2" });
        await context.SaveChangesAsync();

        var suggestion = await sut.SuggestNextFreeIpAsync(network.Id);

        // .1 = Gateway (bezet), .2 = device hierboven (bezet) → eerste vrije is .3
        Assert.Equal("10.0.0.3", suggestion);
    }
}