using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CollegeSupport.Data.Repositories;

public class AppealRepository : IAppealRepository
{
    private readonly AppDbContext _db;

    public AppealRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Appeal>> GetAllAsync(AppealFilter filter)
    {
        IQueryable<Appeal> query = _db.Appeals
            .AsNoTracking()
            .Include(a => a.Employee)
            .Include(a => a.Technician);

        if (filter.Status.HasValue)
            query = query.Where(a => a.Status == filter.Status.Value);

        if (filter.TechnicianId.HasValue)
            query = query.Where(a => a.TechnicianId == filter.TechnicianId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string s = filter.Search.Trim();
            query = query.Where(a => a.Equipment.Contains(s)
                                     || a.Cabinet.Contains(s)
                                     || a.Employee!.FullName.Contains(s)
                                     || a.Employee!.Phone.Contains(s));
        }

        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
    }

    public Task<Appeal?> GetByIdAsync(int id) =>
        _db.Appeals
            .Include(a => a.Employee)
            .Include(a => a.Technician)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task AddAsync(Appeal appeal)
    {
        _db.Appeals.Add(appeal);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Appeal appeal)
    {
        _db.Appeals.Update(appeal);
        await _db.SaveChangesAsync();
    }
}
