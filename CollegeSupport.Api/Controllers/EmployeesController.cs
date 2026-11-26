using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeSupport.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly EmployeeService _service;

    public EmployeesController(EmployeeService service)
    {
        _service = service;
    }

    /// <summary>Список сотрудников с поиском по ФИО, телефону или кабинету.</summary>
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll([FromQuery] string? search)
    {
        var employees = await _service.GetAllAsync(search);
        return employees.Select(x => x.ToDto()).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var employee = await _service.GetByIdAsync(id);
        return employee.ToDto();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeDto dto)
    {
        var employee = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee.ToDto());
    }
}
