using InfrastructureManager.Application.DTOs.Contacts;
using InfrastructureManager.Application.Common;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IContactService
{
    Task<ContactDto?> GetByIdAsync(int id);
    Task<IEnumerable<ContactDto>> GetByDepartmentAsync(int departmentId);
    Task CreateAsync(CreateContactDto dto);
    Task UpdateAsync(UpdateContactDto dto);
    Task DeleteAsync(int id);
    Task<PagedResult<ContactDto>> GetPagedAsync(
        string? search,
        int? departmentId,
        int page,
        int pageSize,
        IReadOnlyCollection<int>? allowedDepartmentIds = null);
}