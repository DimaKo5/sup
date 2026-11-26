using CollegeSupport.Core.Entities;

namespace CollegeSupport.Tests.Core;

public class AppealStatusRulesTests
{
    [Fact]
    public void New_CanGoToInProgressOrRejected()
    {
        Assert.True(AppealStatusRules.CanChange(AppealStatus.New, AppealStatus.InProgress));
        Assert.True(AppealStatusRules.CanChange(AppealStatus.New, AppealStatus.Rejected));
        Assert.False(AppealStatusRules.CanChange(AppealStatus.New, AppealStatus.Closed));
    }

    [Fact]
    public void Resolved_CanOnlyGoToClosed()
    {
        Assert.Equal([AppealStatus.Closed], AppealStatusRules.NextStatuses(AppealStatus.Resolved));
    }

    [Fact]
    public void TerminalStatuses_HaveNoTransitions()
    {
        Assert.Empty(AppealStatusRules.NextStatuses(AppealStatus.Closed));
        Assert.Empty(AppealStatusRules.NextStatuses(AppealStatus.Rejected));
    }

    [Fact]
    public void IsClosed_TrueOnlyForClosedAndRejected()
    {
        Assert.True(AppealStatus.Closed.IsClosed());
        Assert.True(AppealStatus.Rejected.IsClosed());
        Assert.False(AppealStatus.Resolved.IsClosed());
        Assert.False(AppealStatus.InProgress.IsClosed());
    }

    [Theory]
    [InlineData(AppealStatus.New, "Новое")]
    [InlineData(AppealStatus.InProgress, "В работе")]
    [InlineData(AppealStatus.WaitingUser, "Ожидает пользователя")]
    [InlineData(AppealStatus.Resolved, "Решено")]
    [InlineData(AppealStatus.Closed, "Закрыто")]
    [InlineData(AppealStatus.Rejected, "Отклонено")]
    public void ToDisplayName_ReturnsRussianName(AppealStatus status, string expected) =>
        Assert.Equal(expected, status.ToDisplayName());

    [Theory]
    [InlineData(UserRole.Admin, "Администратор")]
    [InlineData(UserRole.Dispatcher, "Диспетчер")]
    [InlineData(UserRole.Technician, "Техник")]
    public void RoleToDisplayName_ReturnsRussianName(UserRole role, string expected) =>
        Assert.Equal(expected, role.ToDisplayName());
}
