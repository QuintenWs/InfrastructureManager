using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
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

        var department = new Department { Name = "Dept", Address = "Addr" };
        var device = new Device { Department = department, Name = "DEV-01", DeviceType = DeviceType.Switch, Status = DeviceStatus.Active };
        context.AddRange(department, device);
        await context.SaveChangesAsync();

        var oldTypeField = new DeviceTypeField { Label = "MAC Address", FieldKey = "mac_address", FieldType = "text" };
        context.Add(oldTypeField);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceId = device.Id, DeviceTypeFieldId = oldTypeField.Id, Value = "AA:BB:CC:DD:EE:FF" });
        await context.SaveChangesAsync();

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

        var department = new Department { Name = "Dept", Address = "Addr" };
        var device = new Device { Department = department, Name = "DEV-01", DeviceType = DeviceType.Switch, Status = DeviceStatus.Active };
        context.AddRange(department, device);
        await context.SaveChangesAsync();

        var field = new DeviceTypeField { Label = "MAC Address", FieldKey = "mac_address", FieldType = "text" };
        context.Add(field);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceId = device.Id, DeviceTypeFieldId = field.Id, Value = "AA:BB:CC:DD:EE:FF" });
        await context.SaveChangesAsync();

        await sut.UpdateAsync(new UpdateDeviceDto
        {
            Id           = device.Id,
            DepartmentId = department.Id,
            Name         = "DEV-01-renamed",
            DeviceType   = DeviceType.Switch, // zelfde type, enkel naam wijzigt
            Status       = DeviceStatus.Active
        });

        Assert.Single(await context.DeviceFieldValues.Where(v => v.DeviceId == device.Id).ToListAsync());
    }
}