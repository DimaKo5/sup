using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Exceptions;
using CollegeSupport.Core.Services;
using CollegeSupport.Tests.Fakes;

namespace CollegeSupport.Tests.Core;

public class AppealServiceTests
{
    private static (AppealService Service, InMemoryEmployeeRepository Employees,
        InMemoryAppealRepository Appeals, InMemoryUserRepository Users) Create()
    {
        var employees = new InMemoryEmployeeRepository();
        var appeals = new InMemoryAppealRepository();
        var users = new InMemoryUserRepository();
        var service = new AppealService(appeals, employees, users);
        return (service, employees, appeals, users);
    }

    private static async Task<Appeal> NewAppeal(AppealService service, InMemoryEmployeeRepository employees,
        AppealStatus status = AppealStatus.New, decimal? cost = null)
    {
        var employee = new Employee { FullName = "Иванов Иван", Phone = "+79001112233", Cabinet = "101" };
        await employees.AddAsync(employee);
        var appeal = await service.CreateAsync(new CreateAppealDto(
            employee.Id, "101", "Ноутбук", "Не включается после попадания воды"));
        appeal.Status = status;
        appeal.Cost = cost;
        return appeal;
    }

    [Fact]
    public async Task Create_ThrowsWhenProblemDescriptionTooShort()
    {
        var (service, employees, _, _) = Create();
        var employee = new Employee { FullName = "Иванов Иван", Phone = "+79001112233", Cabinet = "101" };
        await employees.AddAsync(employee);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(
            new CreateAppealDto(employee.Id, "101", "Ноутбук", "Сломан")));
    }

    [Fact]
    public async Task Create_ThrowsWhenEmployeeMissing()
    {
        var (service, _, _, _) = Create();
        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(
            new CreateAppealDto(999, "101", "Ноутбук", "Не включается после попадания воды")));
    }

    [Fact]
    public async Task Create_SetsNewStatusAndUtcDate()
    {
        var (service, employees, _, _) = Create();
        var appeal = await NewAppeal(service, employees);
        Assert.Equal(AppealStatus.New, appeal.Status);
        Assert.Equal(DateTimeKind.Utc, appeal.CreatedAt.Kind);
    }

    [Fact]
    public async Task ChangeStatus_ThrowsWhenTechnicianNotAssigned()
    {
        var (service, employees, appeals, _) = Create();
        var appeal = await NewAppeal(service, employees);
        appeals.Items.Add(appeal);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ChangeStatusAsync(appeal.Id, AppealStatus.InProgress, null));
        Assert.Contains("назначьте техника", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangeStatus_ThrowsWhenCostMissingForResolved()
    {
        var (service, employees, appeals, users) = Create();
        var appeal = await NewAppeal(service, employees, AppealStatus.InProgress);
        appeals.Items.Add(appeal);
        var technician = new User { Login = "tech", FullName = "Петров", Role = UserRole.Technician };
        await users.AddAsync(technician);
        await service.AssignTechnicianAsync(appeal.Id, technician.Id);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ChangeStatusAsync(appeal.Id, AppealStatus.Resolved, null));
        Assert.Contains("стоимость", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangeStatus_AllowsResolvedWithCost()
    {
        var (service, employees, appeals, users) = Create();
        var appeal = await NewAppeal(service, employees, AppealStatus.InProgress);
        appeals.Items.Add(appeal);
        var technician = new User { Login = "tech", FullName = "Петров", Role = UserRole.Technician };
        await users.AddAsync(technician);
        await service.AssignTechnicianAsync(appeal.Id, technician.Id);

        var result = await service.ChangeStatusAsync(appeal.Id, AppealStatus.Resolved, 1500m);
        Assert.Equal(AppealStatus.Resolved, result.Status);
        Assert.Equal(1500m, result.Cost);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task ChangeStatus_RejectsInvalidTransition()
    {
        var (service, employees, appeals, _) = Create();
        var appeal = await NewAppeal(service, employees, AppealStatus.Closed);
        appeals.Items.Add(appeal);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ChangeStatusAsync(appeal.Id, AppealStatus.InProgress, null));
    }

    [Fact]
    public async Task AssignTechnician_ThrowsForDispatcherRole()
    {
        var (service, employees, appeals, users) = Create();
        var appeal = await NewAppeal(service, employees);
        appeals.Items.Add(appeal);
        var dispatcher = new User { Login = "disp", FullName = "Орлова", Role = UserRole.Dispatcher };
        await users.AddAsync(dispatcher);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.AssignTechnicianAsync(appeal.Id, dispatcher.Id));
    }

    [Fact]
    public async Task AssignTechnician_ThrowsForClosedAppeal()
    {
        var (service, employees, appeals, users) = Create();
        var appeal = await NewAppeal(service, employees, AppealStatus.Closed);
        appeals.Items.Add(appeal);
        var technician = new User { Login = "tech", FullName = "Петров", Role = UserRole.Technician };
        await users.AddAsync(technician);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.AssignTechnicianAsync(appeal.Id, technician.Id));
    }

    [Fact]
    public async Task GetById_ThrowsNotFound()
    {
        var (service, _, _, _) = Create();
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }
}
