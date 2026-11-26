using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Exchange;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeSupport.Api.Controllers;

[ApiController]
[Route("api/exchange")]
[Authorize(Roles = "Admin,Dispatcher")]
public class ExchangeController : ControllerBase
{
    private readonly ExchangeService _service;

    public ExchangeController(ExchangeService service)
    {
        _service = service;
    }

    /// <summary>Импорт сотрудников из CSV-текста (заголовок: ФИО;Телефон;Email;Кабинет).</summary>
    [HttpPost("employees/import")]
    public Task<ImportResultDto> ImportEmployees(ImportCsvDto dto) =>
        _service.ImportEmployeesAsync(dto.Content);

    /// <summary>Выгрузка всех обращений в формате csv, json или xml.</summary>
    [HttpGet("appeals/export")]
    public async Task<IActionResult> ExportAppeals([FromQuery] string format = "csv")
    {
        var file = await _service.ExportAppealsAsync(format);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
