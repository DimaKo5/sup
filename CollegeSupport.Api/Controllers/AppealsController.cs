using CollegeSupport.Api.Infrastructure;
using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Exceptions;
using CollegeSupport.Core.Interfaces;
using CollegeSupport.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeSupport.Api.Controllers;

[ApiController]
[Route("api/appeals")]
[Authorize]
public class AppealsController : ControllerBase
{
    private readonly AppealService _service;

    private readonly ILogger<AppealsController> _logger;

    public AppealsController(AppealService service, ILogger<AppealsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Список обращений с фильтром по статусу, технику и строке поиска.</summary>
    [HttpGet]
    public async Task<ActionResult<List<AppealDto>>> GetAll(
        [FromQuery] AppealStatus? status, [FromQuery] int? technicianId, [FromQuery] string? search)
    {
        var filter = new AppealFilter { Status = status, TechnicianId = technicianId, Search = search };

        // Техник видит только те обращения, которые назначены ему.
        if (User.IsInRole(nameof(UserRole.Technician)))
        {
            int currentId = User.GetUserId();
            if (filter.TechnicianId.HasValue && filter.TechnicianId.Value != currentId)
                return Forbid();
            filter.TechnicianId = currentId;
        }

        var appeals = await _service.GetAllAsync(filter);
        return appeals.Select(a => a.ToDto()).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppealDto>> GetById(int id)
    {
        var appeal = await _service.GetByIdAsync(id);

        if (User.IsInRole(nameof(UserRole.Technician)) && appeal.TechnicianId != User.GetUserId())
            return Forbid();

        return appeal.ToDto();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<AppealDto>> Create(CreateAppealDto dto)
    {
        var appeal = await _service.CreateAsync(dto);
        _logger.LogInformation("Обращение {AppealId} зарегистрировано", appeal.Id);
        return CreatedAtAction(nameof(GetById), new { id = appeal.Id }, appeal.ToDto());
    }

    /// <summary>Смена статуса. Техник может менять статус только своих обращений.</summary>
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<AppealDto>> ChangeStatus(int id, ChangeStatusDto dto)
    {
        if (User.IsInRole(nameof(UserRole.Technician)))
        {
            var current = await _service.GetByIdAsync(id);
            if (current.TechnicianId != User.GetUserId())
                return Forbid();
        }

        var appeal = await _service.ChangeStatusAsync(id, dto.Status, dto.Cost);
        _logger.LogInformation("Обращение {AppealId} переведено в {Status}", id, appeal.Status);
        return appeal.ToDto();
    }

    [HttpPut("{id:int}/technician")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<AppealDto>> AssignTechnician(int id, AssignTechnicianDto dto)
    {
        var appeal = await _service.AssignTechnicianAsync(id, dto.TechnicianId);
        _logger.LogInformation("Обращение {AppealId} назначено технику {TechnicianId}", id, dto.TechnicianId);
        return appeal.ToDto();
    }

    /// <summary>Статистика по всем обращениям или по обращениям одного техника.</summary>
    [HttpGet("statistics")]
    public Task<AppealStatisticsDto> GetStatistics([FromQuery] int? technicianId)
    {
        // Техник получает статистику только по своим обращениям.
        if (User.IsInRole(nameof(UserRole.Technician)))
        {
            int currentId = User.GetUserId();
            if (technicianId.HasValue && technicianId.Value != currentId)
                throw new BusinessRuleException("Нельзя запрашивать статистику другого техника.");
            technicianId = currentId;
        }

        return _service.GetStatisticsAsync(technicianId);
    }
}
