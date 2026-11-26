using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Security;

namespace CollegeSupport.Data;

/// <summary>Заполняет пустую базу демонстрационными данными.</summary>
public static class DbInitializer
{
    public static void Seed(AppDbContext db, IPasswordHasher hasher)
    {
        if (db.Users.Any())
            return; // база уже заполнена

        User NewUser(string login, string password, string fullName, UserRole role)
        {
            var (hash, salt) = hasher.HashPassword(password);
            return new User { Login = login, PasswordHash = hash, PasswordSalt = salt, FullName = fullName, Role = role };
        }

        var admin = NewUser("admin", "Admin123", "Администратор системы", UserRole.Admin);
        var dispatcher = NewUser("dispatcher", "Dispatcher123", "Орлова Анна Сергеевна", UserRole.Dispatcher);
        var petrov = NewUser("technician1", "Tech123", "Петров Пётр Петрович", UserRole.Technician);
        var sidorov = NewUser("technician2", "Tech123", "Сидоров Семён Семёнович", UserRole.Technician);
        db.Users.AddRange(admin, dispatcher, petrov, sidorov);

        var ivanov = new Employee { FullName = "Иванов Иван Иванович", Phone = "+79001112233", Email = "ivanov@example.com", Cabinet = "101" };
        var smirnova = new Employee { FullName = "Смирнова Ольга Петровна", Phone = "+79004445566", Cabinet = "102" };
        var kuznetsov = new Employee { FullName = "Кузнецов Алексей Викторович", Phone = "+79007778899", Email = "kuznetsov@example.com", Cabinet = "203" };
        db.Employees.AddRange(ivanov, smirnova, kuznetsov);

        static DateTime At(int month, int day, int hour) => new(2026, month, day, hour, 0, 0, DateTimeKind.Utc);

        db.Appeals.AddRange(
            new Appeal
            {
                Employee = ivanov, Cabinet = "101", Equipment = "Ноутбук Lenovo IdeaPad 3",
                ProblemDescription = "Не включается после попадания воды на клавиатуру",
                Status = AppealStatus.Closed, Technician = petrov, Cost = 3500m,
                CreatedAt = At(9, 1, 10), CompletedAt = At(9, 4, 10)
            },
            new Appeal
            {
                Employee = smirnova, Cabinet = "102", Equipment = "Смартфон Samsung Galaxy A54",
                ProblemDescription = "Разбит экран, сенсор не реагирует",
                Status = AppealStatus.Resolved, Technician = petrov, Cost = 2000m,
                CreatedAt = At(9, 3, 12), CompletedAt = At(9, 5, 0)
            },
            new Appeal
            {
                Employee = kuznetsov, Cabinet = "203", Equipment = "Принтер HP LaserJet 1020",
                ProblemDescription = "Зажёвывает бумагу при печати",
                Status = AppealStatus.WaitingUser, Technician = petrov,
                CreatedAt = At(9, 10, 9)
            },
            new Appeal
            {
                Employee = ivanov, Cabinet = "101", Equipment = "Планшет Apple iPad 9",
                ProblemDescription = "Не заряжается, разъём болтается",
                Status = AppealStatus.InProgress, Technician = sidorov,
                CreatedAt = At(9, 12, 15)
            },
            new Appeal
            {
                Employee = smirnova, Cabinet = "102", Equipment = "Телевизор LG 43UQ7500",
                ProblemDescription = "Нет изображения, звук есть",
                Status = AppealStatus.Rejected,
                CreatedAt = At(9, 14, 8)
            },
            new Appeal
            {
                Employee = kuznetsov, Cabinet = "203", Equipment = "Ноутбук ASUS VivoBook 15",
                ProblemDescription = "Сильно греется и выключается под нагрузкой",
                Status = AppealStatus.New,
                CreatedAt = At(9, 15, 11)
            },
            new Appeal
            {
                Employee = ivanov, Cabinet = "101", Equipment = "Смартфон Xiaomi Redmi Note 12",
                ProblemDescription = "Быстро разряжается аккумулятор",
                Status = AppealStatus.Resolved, Technician = petrov, Cost = 4500m,
                CreatedAt = At(9, 16, 18), CompletedAt = At(9, 18, 18)
            });

        db.SaveChanges();
    }
}
