using InfrastructureManager.Application.DTOs.Contacts;
using InfrastructureManager.Application.Interfaces.Repositories;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Application.Common;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class ContactService : IContactService
{
    private readonly IContactRepository _repository;
    private readonly IAuditService      _audit;
    private readonly AppDbContext       _context;

    public ContactService(IContactRepository repository, IAuditService audit, AppDbContext context)
    {
        _repository = repository;
        _audit      = audit;
        _context    = context;
    }

    public async Task<PagedResult<ContactDto>> GetPagedAsync(string? search, int? departmentId, int page, int pageSize)
    {
        var query = _context.Contacts
            .Include(x => x.Department).ThenInclude(d => d.Location)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.FirstName.ToLower().Contains(s) ||
                x.LastName.ToLower().Contains(s)  ||
                x.Email.ToLower().Contains(s)     ||
                (x.Role != null && x.Role.ToLower().Contains(s)) ||
                x.Department.Name.ToLower().Contains(s)          ||
                x.Department.Location.Name.ToLower().Contains(s));
        }

        if (departmentId.HasValue)
            query = query.Where(x => x.DepartmentId == departmentId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ContactDto
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
            })
            .ToListAsync();

        return new PagedResult<ContactDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
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
