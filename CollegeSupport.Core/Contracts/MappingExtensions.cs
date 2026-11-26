using CollegeSupport.Core.Entities;

namespace CollegeSupport.Core.Contracts;

/// <summary>Сопоставление сущностей предметной области и объектов передачи данных (DTO).</summary>
public static class MappingExtensions
{
    public static EmployeeDto ToDto(this Employee e) =>
        new(e.Id, e.FullName, e.Phone, e.Email, e.Cabinet);

    public static UserDto ToDto(this User u) =>
        new(u.Id, u.Login, u.FullName, u.Role);

    public static AppealDto ToDto(this Appeal a) => new(
        a.Id,
        a.CreatedAt,
        a.EmployeeId,
        a.Employee?.FullName ?? string.Empty,
        a.Employee?.Phone ?? string.Empty,
        a.Cabinet,
        a.Equipment,
        a.ProblemDescription,
        a.Status,
        a.Status.ToDisplayName(),
        a.TechnicianId,
        a.Technician?.FullName,
        a.Cost,
        a.CompletedAt);
}
