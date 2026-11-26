using CollegeSupport.Core.Entities;

namespace CollegeSupport.Core.Interfaces;

/// <summary>Параметры отбора обращений.</summary>
public class AppealFilter
{
    public AppealStatus? Status { get; set; }
    public int? TechnicianId { get; set; }
    public string? Search { get; set; }
}

public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync(string? search = null);
    Task<Employee?> GetByIdAsync(int id);
    Task<Employee?> GetByPhoneAsync(string phone);
    Task<HashSet<string>> GetAllPhonesAsync();
    Task AddAsync(Employee employee);
    Task AddRangeAsync(IEnumerable<Employee> employees);
}

public interface IAppealRepository
{
    Task<List<Appeal>> GetAllAsync(AppealFilter filter);
    Task<Appeal?> GetByIdAsync(int id);
    Task AddAsync(Appeal appeal);
    Task UpdateAsync(Appeal appeal);
}

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task<List<User>> GetTechniciansAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByLoginAsync(string login);
    Task AddAsync(User user);
}
