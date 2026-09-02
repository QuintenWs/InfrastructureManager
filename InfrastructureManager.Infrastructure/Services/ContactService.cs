using InfrastructureManager.Application.DTOs.Contacts;
using InfrastructureManager.Application.Interfaces.Repositories;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;

namespace InfrastructureManager.Infrastructure.Services;

public class ContactService : IContactService
{
    private readonly IContactRepository _repository;
    private readonly IAuditService      _audit;

    public ContactService(IContactRepository repository, IAuditService audit)
    {
        _repository = repository;
        _audit      = audit;
    }

    public async Task<IEnumerable<ContactDto>> GetAllAsync(string? search = null)
    {
        var items = await _repository.GetAllWithDetailsAsync(search);
        return items.Select(ToDto);
    }

    public async Task<ContactDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetDetailsByIdAsync(id);
        return item == null ? null : ToDto(item);
    }

    public async Task<IEnumerable<ContactDto>> GetByDepartmentAsync(int departmentId)
    {
        var items = await _repository.GetByDepartmentAsync(departmentId);
        return items.Select(ToDto);
    }

    public async Task CreateAsync(CreateContactDto dto)
    {
        var entity = new Contact
        {
            DepartmentId = dto.DepartmentId,
            FirstName    = dto.FirstName,
            LastName     = dto.LastName,
            Email        = dto.Email,
            Phone        = dto.Phone,
            Role         = dto.Role,
            Notes        = dto.Notes
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        await _audit.LogAsync("CREATE", "Contact", entity.Id, entity.FullName,
            newValues: new { entity.FirstName, entity.LastName, entity.Email, entity.Phone, entity.Role, entity.Notes, entity.DepartmentId });
    }

    public async Task UpdateAsync(UpdateContactDto dto)
    {
        var entity = await _repository.GetByIdAsync(dto.Id);
        if (entity == null) return;

        var old = new { entity.FirstName, entity.LastName, entity.Email, entity.Phone, entity.Role, entity.Notes, entity.DepartmentId };

        entity.DepartmentId = dto.DepartmentId;
        entity.FirstName    = dto.FirstName;
        entity.LastName     = dto.LastName;
        entity.Email        = dto.Email;
        entity.Phone        = dto.Phone;
        entity.Role         = dto.Role;
        entity.Notes        = dto.Notes;

        _repository.Update(entity);
        await _repository.SaveChangesAsync();

        await _audit.LogAsync("UPDATE", "Contact", entity.Id, entity.FullName,
            oldValues: old,
            newValues: new { entity.FirstName, entity.LastName, entity.Email, entity.Phone, entity.Role, entity.Notes, entity.DepartmentId });
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return;

        var snapshot = new { entity.FirstName, entity.LastName, entity.Email, entity.Phone, entity.Role, entity.Notes, entity.DepartmentId };

        _repository.Delete(entity);
        await _repository.SaveChangesAsync();

        await _audit.LogAsync("DELETE", "Contact", id,
            $"{snapshot.FirstName} {snapshot.LastName}".Trim(),
            oldValues: snapshot);
    }

    private static ContactDto ToDto(Contact x) => new()
    {
        Id             = x.Id,
        DepartmentId   = x.DepartmentId,
        DepartmentName = x.Department.Name,
        LocationName   = x.Department.Location.Name,
        FirstName      = x.FirstName,
        LastName       = x.LastName,
        Email          = x.Email,
        Phone          = x.Phone,
        Role           = x.Role,
        Notes          = x.Notes,
        CreatedAt      = x.CreatedAt
    };
}
