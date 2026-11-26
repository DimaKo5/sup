using CollegeSupport.Core.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CollegeSupport.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        var connection = (SqliteConnection)Database.GetDbConnection();
        connection.CreateFunction<string, string, bool>(
            "college_contains", (value, search) => value.Contains(search, StringComparison.OrdinalIgnoreCase),
            isDeterministic: true);
    }

    internal static bool ContainsIgnoreCase(string value, string search) =>
        throw new NotSupportedException("Функция применяется в запросах SQLite.");

    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Appeal> Appeals => Set<Appeal>();

    /// <summary>Все даты хранятся в UTC; при чтении из SQLite им возвращается признак DateTimeKind.Utc.</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDbFunction(() => ContainsIgnoreCase(default!, default!))
            .HasName("college_contains");

        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Login).HasMaxLength(50).IsRequired();
            e.HasIndex(u => u.Login).IsUnique();
            e.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Employee>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Phone).IsUnique();
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.Cabinet).HasMaxLength(20).IsRequired();
            e.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Appeal>(e =>
        {
            e.Property(a => a.Cabinet).HasMaxLength(20).IsRequired();
            e.Property(a => a.Equipment).HasMaxLength(100).IsRequired();
            e.Property(a => a.ProblemDescription).HasMaxLength(1000).IsRequired();
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.Cost).HasPrecision(10, 2);

            e.HasOne(a => a.Employee)
                .WithMany(x => x.Appeals)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Technician)
                .WithMany()
                .HasForeignKey(a => a.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
