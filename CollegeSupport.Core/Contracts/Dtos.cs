using System.ComponentModel.DataAnnotations;
using CollegeSupport.Core.Entities;

namespace CollegeSupport.Core.Contracts;

// ---------- Сотрудники ----------
public record EmployeeDto(int Id, string FullName, string Phone, string? Email, string Cabinet);

public record CreateEmployeeDto(
    [Required(ErrorMessage = "Укажите ФИО сотрудника")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "ФИО: от 3 до 150 символов")]
    string FullName,
    [Required(ErrorMessage = "Укажите телефон")]
    string Phone,
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты")]
    string? Email,
    [Required(ErrorMessage = "Укажите кабинет")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "Кабинет: от 1 до 20 символов")]
    string Cabinet);

// ---------- Обращения ----------
public record AppealDto(
    int Id,
    DateTime CreatedAt,
    int EmployeeId,
    string EmployeeName,
    string EmployeePhone,
    string Cabinet,
    string Equipment,
    string ProblemDescription,
    AppealStatus Status,
    string StatusName,
    int? TechnicianId,
    string? TechnicianName,
    decimal? Cost,
    DateTime? CompletedAt);

public record CreateAppealDto(
    [Range(1, int.MaxValue, ErrorMessage = "Выберите сотрудника")]
    int EmployeeId,
    [Required(ErrorMessage = "Укажите кабинет")]
    [StringLength(20)]
    string Cabinet,
    [Required(ErrorMessage = "Укажите оборудование")]
    [StringLength(100)]
    string Equipment,
    [Required(ErrorMessage = "Опишите проблему")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Описание проблемы: от 10 до 1000 символов")]
    string ProblemDescription);

public record ChangeStatusDto(AppealStatus Status, decimal? Cost);

public record AssignTechnicianDto([Range(1, int.MaxValue)] int TechnicianId);

public record AppealStatisticsDto(
    int Total,
    int Open,
    int Completed,
    decimal Revenue,
    decimal AverageCost,
    double AverageResolveDays);

// ---------- Пользователи и вход ----------
public record UserDto(int Id, string Login, string FullName, UserRole Role);

public record CreateUserDto(
    [Required, StringLength(50, MinimumLength = 3)] string Login,
    [Required] string Password,
    [Required, StringLength(150, MinimumLength = 3)] string FullName,
    UserRole Role);

public record LoginRequestDto(
    [Required(ErrorMessage = "Введите логин")] string Login,
    [Required(ErrorMessage = "Введите пароль")] string Password);

public record LoginResponseDto(string Token, DateTime ExpiresAt, UserDto User);

// ---------- Обмен данными ----------
public record ImportCsvDto([Required] string Content);

public record ImportResultDto(int Imported, int Skipped, List<string> Errors);
