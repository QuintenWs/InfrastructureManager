using InfrastructureManager.Application.Common;
using InfrastructureManager.Application.DTOs.Departments;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentDto>> GetAllAsync(string? search = null);
    Task<IEnumerable<DepartmentDto>> GetByLocationAsync(int locationId);
    Task<DepartmentDto?>             GetByIdAsync(int id);
    Task<DepartmentReportDto?>       GetReportAsync(int id);
    Task<IEnumerable<DepartmentPhotoResultDto>> GetPhotosAsync(int departmentId);
    Task CreateAsync(CreateDepartmentDto dto);
    Task UpdateAsync(UpdateDepartmentDto dto);
    Task DeleteAsync(int id);

    /// <summary>allowedDepartmentIds is de echte isolatiegrens (niet locaties) —
    /// zo kan een gebruiker met toegang tot slechts één departement op een
    /// locatie met meerdere departementen niet de andere(n) zien.</summary>
    Task<PagedResult<DepartmentDto>> GetPagedAsync(string? search, int page, int pageSize, IReadOnlyCollection<int>? allowedDepartmentIds = null);
}

public class DepartmentPhotoResultDto
{
    public int     Id      { get; set; }
    public string? Caption { get; set; }
}