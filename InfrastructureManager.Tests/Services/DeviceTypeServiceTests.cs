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
        // Regressietest voor rapportpunt 1.3: een Device Type dat nog in gebruik
        // is mag niet verwijderd worden.
        var sut = CreateSut(out var context);

        var department = await TestData.AddDepartmentAsync(context);
        var definition = await TestData.AddDefinitionAsync(context, DeviceType.Switch, "Switch");
        await TestData.AddDeviceAsync(context, department.Id, DeviceType.Switch, name: "SW-01");
        context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeleteDefinitionAsync(definition.Id));

        Assert.True(await context.DeviceTypeDefinitions.AnyAsync(d => d.Id == definition.Id));
    }

    [Fact]
    public async Task DeleteDefinitionAsync_WithNoDevices_DeletesSuccessfully()
    {
        var sut = CreateSut(out var context);

        var definition = await TestData.AddDefinitionAsync(context, DeviceType.Printer, "Printer");
        context.ChangeTracker.Clear();

        await sut.DeleteDefinitionAsync(definition.Id);

        Assert.False(await context.DeviceTypeDefinitions.AnyAsync(d => d.Id == definition.Id));
    }

    [Fact]
    public async Task DeleteDefinitionAsync_WithFieldValues_RemovesValuesBeforeDeleting()
    {
        // Regressietest voor de tweede helft van 1.3: de NoAction-FK
        // DeviceFieldValue -> DeviceTypeField mag niet meer voor een
        // DbUpdateException zorgen. Het device hieronder heeft bewust een ANDER
        // type (Other) dan de definitie (NAS): anders zou de "nog in gebruik"-
        // blokkade eerst toeslaan. Het is precies het scenario van een
        // verweesde waarde na een type-wissel.
        var sut = CreateSut(out var context);

        var department = await TestData.AddDepartmentAsync(context);
        var definition = await TestData.AddDefinitionAsync(context, DeviceType.NAS, "NAS");
        var field      = await TestData.AddFieldAsync(context, definition.Id, "IP", "ip_address");
        var device     = await TestData.AddDeviceAsync(context, department.Id, DeviceType.Other);
        await TestData.AddFieldValueAsync(context, device.Id, field.Id, "10.0.0.1");
        context.ChangeTracker.Clear();

        await sut.DeleteDefinitionAsync(definition.Id);

        Assert.False(await context.DeviceFieldValues.AnyAsync());
        Assert.False(await context.DeviceTypeFields.AnyAsync());
        Assert.False(await context.DeviceTypeDefinitions.AnyAsync());
    }

    [Fact]
    public async Task ValidateFieldValuesAsync_DuplicateIpOnSameNetwork_ThrowsIpConflictException()
    {
        var sut = CreateSut(out var context);

        var department = await TestData.AddDepartmentAsync(context);
        var network    = await TestData.AddNetworkAsync(context, department.Id, "LAN", "10.0.0.0", "255.255.255.0", 24, "10.0.0.1");
        var definition = await TestData.AddDefinitionAsync(context, DeviceType.Desktop, "Desktop");
        var field      = await TestData.AddFieldAsync(context, definition.Id, "IP Address", "ip_address");
        var existing   = await TestData.AddDeviceAsync(context, department.Id, DeviceType.Desktop, network.Id, "PC-01");
        await TestData.AddFieldValueAsync(context, existing.Id, field.Id, "10.0.0.50");
        context.ChangeTracker.Clear();

        var fieldValues = new Dictionary<int, string> { { field.Id, "10.0.0.50" } };

        var ex = await Assert.ThrowsAsync<IpConflictException>(() =>
            sut.ValidateFieldValuesAsync(network.Id, excludeDeviceId: null, fieldValues));

        Assert.Equal("10.0.0.50", ex.IpAddress);
        Assert.Equal("PC-01", ex.ConflictWith);
    }

    [Fact]
    public async Task ValidateFieldValuesAsync_SameIpOnDifferentNetwork_DoesNotThrow()
    {
        // Het conflict is per Network gescoped, niet globaal (zie rapport 2.4).
        var sut = CreateSut(out var context);

        var department = await TestData.AddDepartmentAsync(context);
        var networkA   = await TestData.AddNetworkAsync(context, department.Id, "LAN A", "10.0.0.0", "255.255.255.0", 24, "10.0.0.1");
        var networkB   = await TestData.AddNetworkAsync(context, department.Id, "LAN B", "10.0.1.0", "255.255.255.0", 24, "10.0.1.1");
        var definition = await TestData.AddDefinitionAsync(context, DeviceType.Desktop, "Desktop");
        var field      = await TestData.AddFieldAsync(context, definition.Id, "IP Address", "ip_address");
        var existing   = await TestData.AddDeviceAsync(context, department.Id, DeviceType.Desktop, networkA.Id, "PC-01");
        await TestData.AddFieldValueAsync(context, existing.Id, field.Id, "10.0.0.50");
        context.ChangeTracker.Clear();

        var fieldValues = new Dictionary<int, string> { { field.Id, "10.0.0.50" } };

        // Zelfde IP-string, maar op netwerk B: geen conflict, dus geen exception.
        await sut.ValidateFieldValuesAsync(networkB.Id, excludeDeviceId: null, fieldValues);
    }

    [Theory]
    [InlineData("number", "not-a-number")]
    [InlineData("date", "not-a-date")]
    [InlineData("mac", "not-a-mac-address")]
    [InlineData("ipv4", "999.999.999.999")]
    public async Task SaveFieldValuesAsync_InvalidFormatForType_ThrowsDeviceFieldValidationException(
        string fieldType, string invalidValue)
    {
        var sut = CreateSut(out var context);

        var department = await TestData.AddDepartmentAsync(context);
        var definition = await TestData.AddDefinitionAsync(context, DeviceType.Other, "TestType");
        var field      = await TestData.AddFieldAsync(context, definition.Id, "Field", "test_field", fieldType);
        var device     = await TestData.AddDeviceAsync(context, department.Id);
        context.ChangeTracker.Clear();

        var fieldValues = new Dictionary<int, string> { { field.Id, invalidValue } };

        await Assert.ThrowsAsync<DeviceFieldValidationException>(() =>
            sut.SaveFieldValuesAsync(device.Id, fieldValues));
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ValidValues_PersistsCorrectly()
    {
        var sut = CreateSut(out var context);

        var department = await TestData.AddDepartmentAsync(context);
        var definition = await TestData.AddDefinitionAsync(context, DeviceType.Other, "TestType");
        var field      = await TestData.AddFieldAsync(context, definition.Id, "Serial", "serial");
        var device     = await TestData.AddDeviceAsync(context, department.Id);
        context.ChangeTracker.Clear();

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

        var department = await TestData.AddDepartmentAsync(context);
        var definition = await TestData.AddDefinitionAsync(context, DeviceType.Other, "TestType");
        var field      = await TestData.AddFieldAsync(context, definition.Id, "Count", "count", "number");
        var device     = await TestData.AddDeviceAsync(context, department.Id);
        await TestData.AddFieldValueAsync(context, device.Id, field.Id, "not-a-number");
        context.ChangeTracker.Clear();

        // Dezelfde (formeel ongeldige) waarde opnieuw meesturen mag niet crashen.
        await sut.SaveFieldValuesAsync(device.Id, new Dictionary<int, string> { { field.Id, "not-a-number" } });
    }
}