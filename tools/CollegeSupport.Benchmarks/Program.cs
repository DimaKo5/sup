using System.Diagnostics;
using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Security;
using CollegeSupport.Data;
using CollegeSupport.Data.Repositories;
using Microsoft.EntityFrameworkCore;

const int rowCount = 10_000;
const int iterations = 7;
string dbPath = Path.Combine(Path.GetTempPath(), $"collegesupport-benchmark-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={dbPath};Pooling=False")
    .Options;

try
{
    await using (var db = new AppDbContext(options))
    {
        await db.Database.MigrateAsync();
        DbInitializer.Seed(db, new Pbkdf2PasswordHasher());
        var employeeIds = await db.Employees.Select(employee => employee.Id).ToListAsync();
        int current = await db.Appeals.CountAsync();
        db.Appeals.AddRange(Enumerable.Range(current, rowCount - current).Select(index => new Appeal
        {
            EmployeeId = employeeIds[index % employeeIds.Count],
            Cabinet = $"B-{index % 100}",
            Equipment = $"Терминал {index}",
            ProblemDescription = "Тестовое обращение для замера",
            Status = AppealStatus.New,
            CreatedAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(index)
        }));
        await db.SaveChangesAsync();
    }

    async Task ReadAppealsAsync(bool tracking)
    {
        await using var db = new AppDbContext(options);
        var appeals = tracking
            ? await db.Appeals.Include(a => a.Employee).Include(a => a.Technician)
                .OrderByDescending(a => a.CreatedAt).ToListAsync()
            : await new AppealRepository(db).GetAllAsync(new());
        if (appeals.Count != rowCount || appeals.Any(a => a.Employee is null))
            throw new InvalidOperationException("В замер попал неполный набор обращений.");
    }

    await ReadAppealsAsync(true);
    await ReadAppealsAsync(false);
    var tracked = new List<double>();
    var readOnly = new List<double>();
    for (int iteration = 0; iteration < iterations; iteration++)
    {
        var elapsed = Stopwatch.StartNew();
        await ReadAppealsAsync(true);
        tracked.Add(elapsed.Elapsed.TotalMilliseconds);
        elapsed.Restart();
        await ReadAppealsAsync(false);
        readOnly.Add(elapsed.Elapsed.TotalMilliseconds);
    }

    var phones = Enumerable.Range(0, rowCount).Select(i => $"+79001{i:D6}").ToList();
    var phoneSet = phones.ToHashSet();
    var queries = Enumerable.Range(0, 1_000).Select(i => phones[i * 10]).ToArray();

    static double MeasureLookups(Func<string, bool> contains, string[] queries)
    {
        var elapsed = Stopwatch.StartNew();
        int found = queries.Count(contains);
        if (found != queries.Length)
            throw new InvalidOperationException("Потеряно совпадение.");
        return elapsed.Elapsed.TotalMilliseconds;
    }

    MeasureLookups(phones.Contains, queries);
    MeasureLookups(phoneSet.Contains, queries);
    var list = new List<double>();
    var set = new List<double>();
    for (int iteration = 0; iteration < iterations; iteration++)
    {
        list.Add(MeasureLookups(phones.Contains, queries));
        set.Add(MeasureLookups(phoneSet.Contains, queries));
    }

    Console.WriteLine(FormattableString.Invariant($"SQLite {rowCount}; tracking={Median(tracked):F3} ms; repository={Median(readOnly):F3} ms"));
    Console.WriteLine(FormattableString.Invariant($"Lookups 1000/{rowCount}; List={Median(list):F3} ms; HashSet={Median(set):F3} ms"));
}
finally
{
    File.Delete(dbPath);
}

static double Median(List<double> samples) => samples.Order().ElementAt(samples.Count / 2);
