using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Exceptions;
using CollegeSupport.Core.Interfaces;

namespace CollegeSupport.Core.Services;

public class EmployeeService
{
    private readonly IEmployeeRepository _employees;

    public EmployeeService(IEmployeeRepository employees)
    {
        _employees = employees;
    }

    public Task<List<Employee>> GetAllAsync(string? search) => _employees.GetAllAsync(search);

    public async Task<Employee> GetByIdAsync(int id) =>
        await _employees.GetByIdAsync(id) ?? throw new NotFoundException("Сотрудник", id);

    public async Task<Employee> CreateAsync(CreateEmployeeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new BusinessRuleException("Укажите ФИО сотрудника.");
        if (string.IsNullOrWhiteSpace(dto.Cabinet))
            throw new BusinessRuleException("Укажите кабинет сотрудника.");

        string phone = PhoneNormalizer.Normalize(dto.Phone);
        if (await _employees.GetByPhoneAsync(phone) is not null)
            throw new BusinessRuleException($"Сотрудник с телефоном {phone} уже зарегистрирован.");

        var employee = new Employee
        {
            FullName = dto.FullName.Trim(),
            Phone = phone,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            Cabinet = dto.Cabinet.Trim()
        };
        await _employees.AddAsync(employee);
        return employee;
    }
}
