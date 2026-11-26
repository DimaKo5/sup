using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Services;

namespace CollegeSupport.Tests.Core;

public class AppealStatisticsCalculatorTests
{
    private static List<Appeal> Seed() =>
    [
        new Appeal { Status = AppealStatus.Closed, Cost = 3500m, CreatedAt = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), CompletedAt = new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc) },
        new Appeal { Status = AppealStatus.Resolved, Cost = 2000m, CreatedAt = new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc), CompletedAt = new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc) },
        new Appeal { Status = AppealStatus.WaitingUser, CreatedAt = new DateTime(2026, 9, 10, 9, 0, 0, DateTimeKind.Utc) },
        new Appeal { Status = AppealStatus.InProgress, CreatedAt = new DateTime(2026, 9, 12, 15, 0, 0, DateTimeKind.Utc) },
        new Appeal { Status = AppealStatus.Rejected, CreatedAt = new DateTime(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc) },
        new Appeal { Status = AppealStatus.New, CreatedAt = new DateTime(2026, 9, 15, 11, 0, 0, DateTimeKind.Utc) },
        new Appeal { Status = AppealStatus.Resolved, Cost = 4500m, CreatedAt = new DateTime(2026, 9, 16, 18, 0, 0, DateTimeKind.Utc), CompletedAt = new DateTime(2026, 9, 18, 18, 0, 0, DateTimeKind.Utc) }
    ];

    [Fact]
    public void Calculate_ReturnsAllExpectedValues()
    {
        var result = AppealStatisticsCalculator.Calculate(Seed());
        Assert.Equal(7, result.Total);
        Assert.Equal(3, result.Open);
        Assert.Equal(3, result.Completed);
        Assert.Equal(10000m, result.Revenue);
        Assert.Equal(3333.33m, result.AverageCost);
        Assert.Equal(2.2, result.AverageResolveDays);
    }

    [Fact]
    public void Calculate_ForEmptyList_ReturnsZeros()
    {
        var result = AppealStatisticsCalculator.Calculate([]);
        Assert.Equal(0, result.Total);
        Assert.Equal(0m, result.Revenue);
        Assert.Equal(0m, result.AverageCost);
        Assert.Equal(0, result.AverageResolveDays);
    }

    [Fact]
    public void Calculate_UsesFractionalDays()
    {
        var appeals = new List<Appeal>
        {
            new() { Status = AppealStatus.Resolved, Cost = 100m,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CompletedAt = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc) }
        };
        Assert.Equal(1.5, AppealStatisticsCalculator.Calculate(appeals).AverageResolveDays);
    }

    [Theory]
    [InlineData(AppealStatus.New, true)]
    [InlineData(AppealStatus.InProgress, true)]
    [InlineData(AppealStatus.WaitingUser, true)]
    [InlineData(AppealStatus.Resolved, false)]
    [InlineData(AppealStatus.Closed, false)]
    [InlineData(AppealStatus.Rejected, false)]
    public void OpenStatuses_AreCountedCorrectly(AppealStatus status, bool isOpen)
    {
        var appeals = new List<Appeal> { new() { Status = status } };
        var result = AppealStatisticsCalculator.Calculate(appeals);
        Assert.Equal(isOpen ? 1 : 0, result.Open);
    }
}
