using InfrastructureManager.Application.DTOs.Contacts;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IContactService
{
    Task<IEnumerable<ContactDto>> GetAllAsync(string? search = null);
    Task<ContactDto?> GetByIdAsync(int id);
    Task<IEnumerable<ContactDto>> GetByDepartmentAsync(int departmentId);
    Task CreateAsync(CreateContactDto dto);
    Task UpdateAsync(UpdateContactDto dto);
    Task DeleteAsync(int id);
}
