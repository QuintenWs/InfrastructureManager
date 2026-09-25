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

public class DeviceTypeServiceTests
{
    private static DeviceTypeService CreateSut(out AppDbContext context)
    {
        context = InMemoryDbContextFactory.Create();
        var auditMock = new Mock<IAuditService>();
        return new DeviceTypeService(context, auditMock.Object);
    }

    [Fact]
    public async Task DeleteDefinitionAsync_WithExistingDevices_ThrowsAndDoesNotDelete()
    {
        // Regressietest voor rapportpunt 1.3: verwijderen van een Device Type
        // dat nog in gebruik is, moet geblokkeerd worden i.p.v. te crashen of
        // stilzwijgend toestellen te "verwezen".
        var sut = CreateSut(out var context);

        var location   = new Location { Name = "Loc", City = "City", Country = "Country" };
        var department = new Department { Location = location, Name = "Dept", Address = "Addr" };
        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Switch, Name = "Switch" };
        var device     = new Device { Department = department, Name = "SW-01", DeviceType = DeviceType.Switch, Status = DeviceStatus.Active };

        context.AddRange(location, department, definition, device);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeleteDefinitionAsync(definition.Id));

        Assert.True(await context.DeviceTypeDefinitions.AnyAsync(d => d.Id == definition.Id));
    }

    [Fact]
    public async Task DeleteDefinitionAsync_WithNoDevices_DeletesSuccessfully()
    {
        var sut = CreateSut(out var context);

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Printer, Name = "Printer" };
        context.Add(definition);
        await context.SaveChangesAsync();

        await sut.DeleteDefinitionAsync(definition.Id);

        Assert.False(await context.DeviceTypeDefinitions.AnyAsync(d => d.Id == definition.Id));
    }

    [Fact]
    public async Task DeleteDefinitionAsync_WithFieldValues_RemovesValuesBeforeDeleting()
    {
        // Regressietest voor de tweede helft van 1.3: de NoAction-FK op
        // DeviceFieldValues -> DeviceTypeField mocht nooit meer een
        // onafgevangen DbUpdateException geven, zelfs met historische waarden.
        var sut = CreateSut(out var context);

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.NAS, Name = "NAS" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "IP", FieldKey = "ip_address", FieldType = "text" };
        context.AddRange(definition, field);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceTypeFieldId = field.Id, DeviceId = 999, Value = "10.0.0.1" });
        await context.SaveChangesAsync();

        await sut.DeleteDefinitionAsync(definition.Id);

        Assert.False(await context.DeviceFieldValues.AnyAsync());
    }

    [Fact]
    public async Task ValidateFieldValuesAsync_DuplicateIpOnSameNetwork_ThrowsIpConflictException()
    {
        var sut = CreateSut(out var context);

        var location    = new Location { Name = "Loc", City = "City", Country = "Country" };
        var department  = new Department { Location = location, Name = "Dept", Address = "Addr" };
        var network      = new Network { Department = department, Name = "LAN", NetworkAddress = "10.0.0.0", SubnetMask = "255.255.255.0", Cidr = 24, Gateway = "10.0.0.1", PrimaryDns = "8.8.8.8" };
        var definition   = new DeviceTypeDefinition { DeviceType = DeviceType.Desktop, Name = "Desktop" };
        var field        = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "IP Address", FieldKey = "ip_address", FieldType = "text" };
        var existingDevice = new Device { Department = department, Network = network, Name = "PC-01", DeviceType = DeviceType.Desktop, Status = DeviceStatus.Active };

        context.AddRange(location, department, network, definition, field, existingDevice);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceTypeFieldId = field.Id, DeviceId = existingDevice.Id, Value = "10.0.0.50" });
        await context.SaveChangesAsync();

        var fieldValues = new Dictionary<int, string> { { field.Id, "10.0.0.50" } };

        var ex = await Assert.ThrowsAsync<IpConflictException>(() =>
            sut.ValidateFieldValuesAsync(network.Id, excludeDeviceId: null, fieldValues));

        Assert.Equal("10.0.0.50", ex.IpAddress);
        Assert.Equal("PC-01", ex.ConflictWith);
    }

    [Fact]
    public async Task ValidateFieldValuesAsync_SameIpOnDifferentNetwork_DoesNotThrow()
    {
        // Twee netwerken mogen legitiem hetzelfde privé-IP gebruiken — het
        // conflict is per Network geschoped, niet globaal (zie rapport 2.4).
        var sut = CreateSut(out var context);

        var location   = new Location { Name = "Loc", City = "City", Country = "Country" };
        var department = new Department { Location = location, Name = "Dept", Address = "Addr" };
        var networkA   = new Network { Department = department, Name = "LAN A", NetworkAddress = "10.0.0.0", SubnetMask = "255.255.255.0", Cidr = 24, Gateway = "10.0.0.1", PrimaryDns = "8.8.8.8" };
        var networkB   = new Network { Department = department, Name = "LAN B", NetworkAddress = "10.0.1.0", SubnetMask = "255.255.255.0", Cidr = 24, Gateway = "10.0.1.1", PrimaryDns = "8.8.8.8" };
        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Desktop, Name = "Desktop" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "IP Address", FieldKey = "ip_address", FieldType = "text" };
        var existingDevice = new Device { Department = department, Network = networkA, Name = "PC-01", DeviceType = DeviceType.Desktop, Status = DeviceStatus.Active };

        context.AddRange(location, department, networkA, networkB, definition, field, existingDevice);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceTypeFieldId = field.Id, DeviceId = existingDevice.Id, Value = "10.0.0.50" });
        await context.SaveChangesAsync();

        var fieldValues = new Dictionary<int, string> { { field.Id, "10.0.0.50" } };

        await sut.ValidateFieldValuesAsync(networkB.Id, excludeDeviceId: null, fieldValues);
        // Geen exception = geslaagd
    }

    [Theory]
    [InlineData("number", "not-a-number")]
    [InlineData("date", "not-a-date")]
    [InlineData("mac", "not-a-mac-address")]
    [InlineData("ipv4", "999.999.999.999")]
    public async Task SaveFieldValuesAsync_InvalidFormatForType_ThrowsDeviceFieldValidationException(string fieldType, string invalidValue)
    {
        var sut = CreateSut(out var context);

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Other, Name = "TestType" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "Field", FieldKey = "test_field", FieldType = fieldType };
        var device     = new Device { Name = "DEV-01", DeviceType = DeviceType.Other, Status = DeviceStatus.Active };

        context.AddRange(definition, field, device);
        await context.SaveChangesAsync();

        var fieldValues = new Dictionary<int, string> { { field.Id, invalidValue } };

        await Assert.ThrowsAsync<DeviceFieldValidationException>(() =>
            sut.SaveFieldValuesAsync(device.Id, fieldValues));
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ValidValues_PersistsCorrectly()
    {
        var sut = CreateSut(out var context);

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Other, Name = "TestType" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "Serial", FieldKey = "serial", FieldType = "text" };
        var device     = new Device { Name = "DEV-01", DeviceType = DeviceType.Other, Status = DeviceStatus.Active };

        context.AddRange(definition, field, device);
        await context.SaveChangesAsync();

        await sut.SaveFieldValuesAsync(device.Id, new Dictionary<int, string> { { field.Id, "SN-12345" } });

        var saved = await context.DeviceFieldValues.SingleAsync(v => v.DeviceId == device.Id);
        Assert.Equal("SN-12345", saved.Value);
    }

    [Fact]
    public async Task SaveFieldValuesAsync_UnchangedValue_SkipsFormatValidation()
    {
        // Legacy-data van vóór de 6.5-validatie bestond mag een verder
        // ongerelateerde bewerking niet blokkeren (zie rapport 6.5).
        var sut = CreateSut(out var context);

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Other, Name = "TestType" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "Count", FieldKey = "count", FieldType = "number" };
        var device     = new Device { Name = "DEV-01", DeviceType = DeviceType.Other, Status = DeviceStatus.Active };

        context.AddRange(definition, field, device);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceTypeFieldId = field.Id, DeviceId = device.Id, Value = "not-a-number" });
        await context.SaveChangesAsync();

        // Zelfde (formeel ongeldige) waarde opnieuw meesturen — mag niet crashen.
        await sut.SaveFieldValuesAsync(device.Id, new Dictionary<int, string> { { field.Id, "not-a-number" } });
    }
}