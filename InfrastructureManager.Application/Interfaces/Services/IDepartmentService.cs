using InfrastructureManager.Application.Common;
using InfrastructureManager.Application.DTOs.Departments;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentDto>> GetAllAsync(string? search = null);
    Task<PagedResult<DepartmentDto>> GetPagedAsync(string? search, int page, int pageSize);
    Task<IEnumerable<DepartmentDto>> GetByLocationAsync(int locationId);
    Task<DepartmentDto?>             GetByIdAsync(int id);
    Task<DepartmentReportDto?>       GetReportAsync(int id);
    Task<IEnumerable<DepartmentPhotoResultDto>> GetPhotosAsync(int departmentId);
    Task CreateAsync(CreateDepartmentDto dto);
    Task UpdateAsync(UpdateDepartmentDto dto);
    Task DeleteAsync(int id);
}

public class DepartmentPhotoResultDto
{
    public int     Id      { get; set; }
    public string? Caption { get; set; }
}