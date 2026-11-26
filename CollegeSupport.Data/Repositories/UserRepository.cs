using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CollegeSupport.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<User>> GetAllAsync() =>
        _db.Users.OrderBy(u => u.FullName).ToListAsync();

    public Task<List<User>> GetTechniciansAsync() =>
        _db.Users.Where(u => u.Role == UserRole.Technician && u.IsActive)
                 .OrderBy(u => u.FullName)
                 .ToListAsync();

    public Task<User?> GetByIdAsync(int id) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByLoginAsync(string login) =>
        _db.Users.FirstOrDefaultAsync(u => u.Login == login);

    public async Task AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }
}
