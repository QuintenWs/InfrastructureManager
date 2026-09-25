using InfrastructureManager.Domain.Entities;

namespace InfrastructureManager.Application.Common;

/// <summary>
/// Eén centrale definitie van "wat telt als een IP-adresveld" — gebruikt
/// door DeviceService (weergave), TopologyService (weergave) en
/// DeviceTypeService (conflictdetectie). Voorheen stond deze logica drie
/// keer apart uitgeschreven, met kleine, ongewilde verschillen tussen de
/// varianten.
/// </summary>
public static class DeviceFieldHelpers
{
    public static bool IsIpAddressField(DeviceTypeField field) =>
        field.FieldType == "ipv4" || field.FieldType == "ipv6" || field.FieldKey == "ip_address";

    /// <summary>Vereist dat Device.FieldValues (met .Field erbij) al geladen is.</summary>
    public static string? GetIpAddress(this Device device) =>
        device.FieldValues
            .FirstOrDefault(v => v.Field != null && IsIpAddressField(v.Field))
            ?.Value;
}