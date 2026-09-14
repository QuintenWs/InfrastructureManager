namespace InfrastructureManager.Application.DTOs.AccessGroups;

public class AccessGroupSummaryDto
{
    public int     Id                   { get; set; }
    public string  Name                 { get; set; } = string.Empty;
    public string? Description          { get; set; }
    public bool    CanViewHistory       { get; set; }
    public int     DepartmentGrantCount { get; set; }
    public int     LocationGrantCount   { get; set; }
    public int     MemberCount          { get; set; }
}

public class AccessGroupDetailsDto : AccessGroupSummaryDto
{
    public IReadOnlyList<AccessGrantDto>       Grants  { get; set; } = new List<AccessGrantDto>();
    public IReadOnlyList<AccessGroupMemberDto> Members { get; set; } = new List<AccessGroupMemberDto>();
}

/// <summary>
/// Eén toegekende scope — ofwel een departement, ofwel een volledige
/// locatie. Gedeelde vorm voor zowel groep-grants als individuele
/// gebruikers-grants (zelfde weergave-behoefte).
/// </summary>
public class AccessGrantDto
{
    public int     Id                     { get; set; }
    public int?    DepartmentId           { get; set; }
    public string? DepartmentName         { get; set; }
    public string? DepartmentLocationName { get; set; }
    public int?    LocationId             { get; set; }
    public string? LocationName           { get; set; }
}

public class AccessGroupMemberDto
{
    public string UserId      { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class CreateAccessGroupDto
{
    public string  Name           { get; set; } = string.Empty;
    public string? Description    { get; set; }
    public bool    CanViewHistory { get; set; }
}

public class UpdateAccessGroupDto
{
    public int     Id             { get; set; }
    public string  Name           { get; set; } = string.Empty;
    public string? Description    { get; set; }
    public bool    CanViewHistory { get; set; }
}