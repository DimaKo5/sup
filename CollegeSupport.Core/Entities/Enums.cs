namespace CollegeSupport.Core.Entities;

/// <summary>Роль пользователя системы.</summary>
public enum UserRole
{
    Admin = 1,       // Администратор
    Dispatcher = 2,  // Диспетчер
    Technician = 3   // Техник
}

/// <summary>Статус обращения в техническую поддержку колледжа.</summary>
public enum AppealStatus
{
    New = 1,           // Новое
    InProgress = 2,    // В работе
    WaitingUser = 3,   // Ожидает пользователя
    Resolved = 4,      // Решено
    Closed = 5,        // Закрыто
    Rejected = 6       // Отклонено
}
