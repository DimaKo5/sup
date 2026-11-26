using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Exceptions;
using CollegeSupport.Core.Interfaces;

namespace CollegeSupport.Core.Services;

public class AppealService
{
    private readonly IAppealRepository _appeals;
    private readonly IEmployeeRepository _employees;
    private readonly IUserRepository _users;

    public AppealService(
        IAppealRepository appeals,
        IEmployeeRepository employees,
        IUserRepository users)
    {
        _appeals = appeals;
        _employees = employees;
        _users = users;
    }

    public Task<List<Appeal>> GetAllAsync(AppealFilter filter) => _appeals.GetAllAsync(filter);

    public async Task<Appeal> GetByIdAsync(int id) =>
        await _appeals.GetByIdAsync(id) ?? throw new NotFoundException("Обращение", id);

    public async Task<Appeal> CreateAsync(CreateAppealDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Cabinet))
            throw new BusinessRuleException("Укажите кабинет.");
        if (string.IsNullOrWhiteSpace(dto.Equipment))
            throw new BusinessRuleException("Укажите оборудование.");
        if (string.IsNullOrWhiteSpace(dto.ProblemDescription) || dto.ProblemDescription.Trim().Length < 10)
            throw new BusinessRuleException("Описание проблемы должно содержать не менее 10 символов.");

        var employee = await _employees.GetByIdAsync(dto.EmployeeId)
                       ?? throw new NotFoundException("Сотрудник", dto.EmployeeId);

        var appeal = new Appeal
        {
            EmployeeId = employee.Id,
            Employee = employee,
            Cabinet = dto.Cabinet.Trim(),
            Equipment = dto.Equipment.Trim(),
            ProblemDescription = dto.ProblemDescription.Trim(),
            Status = AppealStatus.New,
            CreatedAt = DateTime.UtcNow
        };
        await _appeals.AddAsync(appeal);
        return appeal;
    }

    public async Task<Appeal> ChangeStatusAsync(int id, AppealStatus newStatus, decimal? cost)
    {
        var appeal = await GetByIdAsync(id);

        if (!AppealStatusRules.CanChange(appeal.Status, newStatus))
            throw new BusinessRuleException(
                $"Нельзя перевести обращение из статуса «{appeal.Status.ToDisplayName()}» " +
                $"в статус «{newStatus.ToDisplayName()}».");

        if (newStatus == AppealStatus.InProgress && appeal.TechnicianId is null)
            throw new BusinessRuleException("Перед началом работ назначьте техника.");

        if (newStatus == AppealStatus.Resolved)
        {
            if (cost is null or <= 0)
                throw new BusinessRuleException("Для решения обращения укажите стоимость работ больше 0.");
            appeal.Cost = cost;
            appeal.CompletedAt = DateTime.UtcNow;
        }

        appeal.Status = newStatus;
        await _appeals.UpdateAsync(appeal);
        return appeal;
    }

    public async Task<Appeal> AssignTechnicianAsync(int id, int technicianId)
    {
        var appeal = await GetByIdAsync(id);
        if (appeal.Status.IsClosed())
            throw new BusinessRuleException("Нельзя назначить техника на закрытое обращение.");

        var technician = await _users.GetByIdAsync(technicianId)
                         ?? throw new NotFoundException("Пользователь", technicianId);
        if (technician.Role != UserRole.Technician || !technician.IsActive)
            throw new BusinessRuleException("Назначить можно только активного пользователя с ролью «Техник».");

        appeal.TechnicianId = technician.Id;
        appeal.Technician = technician;
        await _appeals.UpdateAsync(appeal);
        return appeal;
    }

    public async Task<AppealStatisticsDto> GetStatisticsAsync(int? technicianId)
    {
        var appeals = await _appeals.GetAllAsync(new AppealFilter { TechnicianId = technicianId });
        return AppealStatisticsCalculator.Calculate(appeals);
    }
}
