using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CollegeSupport.Data.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _db;

    public EmployeeRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Employee>> GetAllAsync(string? search = null)
    {
        IQueryable<Employee> query = _db.Employees;
        if (!string.IsNullOrWhiteSpace(search))
        {
            string s = search.Trim();
            query = query.Where(x => x.FullName.Contains(s) || x.Phone.Contains(s) || x.Cabinet.Contains(s));
        }
        return await query.OrderBy(x => x.FullName).ToListAsync();
    }

    public Task<Employee?> GetByIdAsync(int id) =>
        _db.Employees.FirstOrDefaultAsync(x => x.Id == id);

    public Task<Employee?> GetByPhoneAsync(string phone) =>
        _db.Employees.FirstOrDefaultAsync(x => x.Phone == phone);

    public async Task<HashSet<string>> GetAllPhonesAsync() =>
        (await _db.Employees.Select(x => x.Phone).ToListAsync()).ToHashSet();

    public async Task AddAsync(Employee employee)
    {
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Employee> employees)
    {
        _db.Employees.AddRange(employees);
        await _db.SaveChangesAsync();
    }
}
