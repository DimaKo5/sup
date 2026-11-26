namespace CollegeSupport.Core.Entities;

/// <summary>Правила переходов между статусами обращения и русские названия перечислений.</summary>
public static class AppealStatusRules
{
    private static readonly Dictionary<AppealStatus, AppealStatus[]> Allowed = new()
    {
        [AppealStatus.New] = [AppealStatus.InProgress, AppealStatus.Rejected],
        [AppealStatus.InProgress] = [AppealStatus.WaitingUser, AppealStatus.Resolved, AppealStatus.Rejected],
        [AppealStatus.WaitingUser] = [AppealStatus.InProgress, AppealStatus.Resolved, AppealStatus.Rejected],
        [AppealStatus.Resolved] = [AppealStatus.Closed],
        [AppealStatus.Closed] = [],
        [AppealStatus.Rejected] = []
    };

    public static bool CanChange(AppealStatus from, AppealStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlyList<AppealStatus> NextStatuses(AppealStatus from) =>
        Allowed.TryGetValue(from, out var targets) ? targets : [];

    public static bool IsClosed(this AppealStatus status) =>
        status is AppealStatus.Closed or AppealStatus.Rejected;

    public static string ToDisplayName(this AppealStatus status) => status switch
    {
        AppealStatus.New => "Новое",
        AppealStatus.InProgress => "В работе",
        AppealStatus.WaitingUser => "Ожидает пользователя",
        AppealStatus.Resolved => "Решено",
        AppealStatus.Closed => "Закрыто",
        AppealStatus.Rejected => "Отклонено",
        _ => status.ToString()
    };

    public static string ToDisplayName(this UserRole role) => role switch
    {
        UserRole.Admin => "Администратор",
        UserRole.Dispatcher => "Диспетчер",
        UserRole.Technician => "Техник",
        _ => role.ToString()
    };
}
