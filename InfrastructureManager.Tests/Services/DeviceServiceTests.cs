using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Services;
using InfrastructureManager.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InfrastructureManager.Tests.Services;

public class DeviceServiceTests
{
    private static DeviceService CreateSut(out AppDbContext context)
    {
        context = InMemoryDbContextFactory.Create();
        var auditMock = new Mock<IAuditService>();
        return new DeviceService(auditMock.Object, context);
    }

    [Fact]
    public async Task UpdateAsync_ChangingDeviceType_ClearsOldFieldValues()
    {
        // Regressietest voor rapportpunt 1.2.
        var sut = CreateSut(out var context);

        var department = await TestData.AddDepartmentAsync(context);
        var definition = await TestData.AddDefinitionAsync(context, DeviceType.Switch, "Switch");
        var field      = await TestData.AddFieldAsync(context, definition.Id, "MAC Address", "mac_address");
        var device     = await TestData.AddDeviceAsync(context, department.Id, DeviceType.Switch);
        await TestData.AddFieldValueAsync(context, device.Id, field.Id, "AA:BB:CC:DD:EE:FF");
        context.ChangeTracker.Clear();

        await sut.UpdateAsync(new UpdateDeviceDto
        {
            Id           = device.Id,
            DepartmentId = department.Id,
            Name         = device.Name,
            DeviceType   = DeviceType.Printer, // wisselt van type
            Status       = DeviceStatus.Active
        });

        Assert.Empty(await context.DeviceFieldValues.Where(v => v.DeviceId == device.Id).ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_SameDeviceType_KeepsFieldValues()
    {
        var sut = CreateSut(out var context);

        var department = await TestData.AddDepartmentAsync(context);
        var definition = await TestData.AddDefinitionAsync(context, DeviceType.Switch, "Switch");
        var field      = await TestData.AddFieldAsync(context, definition.Id, "MAC Address", "mac_address");
        var device     = await TestData.AddDeviceAsync(context, department.Id, DeviceType.Switch);
        await TestData.AddFieldValueAsync(context, device.Id, field.Id, "AA:BB:CC:DD:EE:FF");
        context.ChangeTracker.Clear();

        await sut.UpdateAsync(new UpdateDeviceDto
        {
            Id           = device.Id,
            DepartmentId = department.Id,
            Name         = "DEV-01-renamed",
            DeviceType   = DeviceType.Switch, // zelfde type, enkel de naam wijzigt
            Status       = DeviceStatus.Active
        });

        Assert.Single(await context.DeviceFieldValues.Where(v => v.DeviceId == device.Id).ToListAsync());
    }
}