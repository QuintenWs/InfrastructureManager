using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Application.Filters;

public class DeviceFilter
{
    public string?      Search       { get; set; }
    public DeviceType?  DeviceType   { get; set; }
    public DeviceStatus? Status      { get; set; }
    public int?         LocationId   { get; set; }
    public int?         DepartmentId { get; set; }

    /// <summary>
    /// Departementen waartoe de aanvragende gebruiker toegang heeft. Null =
    /// geen beperking (Admin). Dit is de eigenlijke isolatiegrens tussen
    /// departementen — niet Location (een locatie kan meerdere departementen
    /// bevatten die niet noodzakelijk allemaal toegankelijk zijn).
    /// </summary>
    public IReadOnlyCollection<int>? AllowedDepartmentIds { get; set; }
}