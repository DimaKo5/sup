using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Interfaces;

namespace CollegeSupport.Tests.Fakes;

public sealed class InMemoryEmployeeRepository : IEmployeeRepository
{
    public List<Employee> Items { get; } = new();

    public Task<List<Employee>> GetAllAsync(string? search = null)
    {
        var query = Items.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.FullName.Contains(search) || x.Phone.Contains(search) || x.Cabinet.Contains(search));
        return Task.FromResult(query.OrderBy(x => x.FullName).ToList());
    }

    public Task<Employee?> GetByIdAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
    public Task<Employee?> GetByPhoneAsync(string phone) => Task.FromResult(Items.FirstOrDefault(x => x.Phone == phone));
    public Task<HashSet<string>> GetAllPhonesAsync() => Task.FromResult(Items.Select(x => x.Phone).ToHashSet());

    public Task AddAsync(Employee employee)
    {
        employee.Id = Items.Count == 0 ? 1 : Items.Max(x => x.Id) + 1;
        Items.Add(employee);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Employee> employees)
    {
        foreach (var employee in employees)
        {
            employee.Id = Items.Count == 0 ? 1 : Items.Max(x => x.Id) + 1;
            Items.Add(employee);
        }
        return Task.CompletedTask;
    }
}

public sealed class InMemoryAppealRepository : IAppealRepository
{
    public List<Appeal> Items { get; } = new();

    public Task<List<Appeal>> GetAllAsync(AppealFilter filter)
    {
        var query = Items.AsQueryable();
        if (filter.Status.HasValue)
            query = query.Where(a => a.Status == filter.Status.Value);
        if (filter.TechnicianId.HasValue)
            query = query.Where(a => a.TechnicianId == filter.TechnicianId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string s = filter.Search.Trim();
            query = query.Where(a => a.Equipment.Contains(s) || a.Cabinet.Contains(s)
                                     || (a.Employee != null && a.Employee.FullName.Contains(s)));
        }
        return Task.FromResult(query.OrderByDescending(a => a.CreatedAt).ToList());
    }

    public Task<Appeal?> GetByIdAsync(int id) => Task.FromResult(Items.FirstOrDefault(a => a.Id == id));

    public Task AddAsync(Appeal appeal)
    {
        appeal.Id = Items.Count == 0 ? 1 : Items.Max(x => x.Id) + 1;
        Items.Add(appeal);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Appeal appeal) => Task.CompletedTask;
}

public sealed class InMemoryUserRepository : IUserRepository
{
    public List<User> Items { get; } = new();

    public Task<List<User>> GetAllAsync() => Task.FromResult(Items.OrderBy(u => u.FullName).ToList());

    public Task<List<User>> GetTechniciansAsync() =>
        Task.FromResult(Items.Where(u => u.Role == UserRole.Technician && u.IsActive).ToList());

    public Task<User?> GetByIdAsync(int id) => Task.FromResult(Items.FirstOrDefault(u => u.Id == id));
    public Task<User?> GetByLoginAsync(string login) => Task.FromResult(Items.FirstOrDefault(u => u.Login == login));

    public Task AddAsync(User user)
    {
        user.Id = Items.Count == 0 ? 1 : Items.Max(x => x.Id) + 1;
        Items.Add(user);
        return Task.CompletedTask;
    }
}
