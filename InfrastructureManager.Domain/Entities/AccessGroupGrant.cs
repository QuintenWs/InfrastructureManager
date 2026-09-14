namespace InfrastructureManager.Domain.Entities;

/// <summary>
/// Eén toegekende scope voor een AccessGroup: ofwel één specifiek
/// departement, ofwel een volledige locatie (= alle departementen die nu,
/// of later, aan die locatie hangen). Exact één van DepartmentId/LocationId
/// staat ingevuld — dit wordt afgedwongen door de service-methodes die deze
/// rijen aanmaken (AddDepartmentGrantToGroupAsync / AddLocationGrantToGroupAsync),
/// nooit door een generieke "maak een grant aan"-methode.
/// </summary>
public class AccessGroupGrant
{
    public int Id { get; set; }

    public int AccessGroupId { get; set; }
    public AccessGroup AccessGroup { get; set; } = null!;

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int? LocationId { get; set; }
    public Location? Location { get; set; }
}