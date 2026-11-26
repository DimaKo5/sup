using System.Diagnostics;
using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Entities;

namespace CollegeSupport.Core.Services;

/// <summary>Расчёт статистики по обращениям технической поддержки.</summary>
public static class AppealStatisticsCalculator
{
    public static AppealStatisticsDto Calculate(IReadOnlyList<Appeal> appeals)
    {
        int total = appeals.Count;
        int open = 0;
        int completed = 0;
        decimal revenue = 0;
        double totalDays = 0;

        for (int i = 0; i < appeals.Count; i++)
        {
            var a = appeals[i];
            if (a.Status is AppealStatus.New or AppealStatus.InProgress or AppealStatus.WaitingUser)
                open++;
            if (a.Status is AppealStatus.Resolved or AppealStatus.Closed)
            {
                completed++;
                revenue += a.Cost ?? 0;
                if (a.CompletedAt.HasValue)
                    totalDays += (a.CompletedAt.Value - a.CreatedAt).TotalDays;
            }
        }

        Debug.Assert(open + completed <= total, "Открытых и завершённых обращений больше, чем всего");

        Debug.Assert(totalDays >= 0, "Суммарный срок решения отрицательный");

        decimal averageCost = completed == 0 ? 0 : revenue / completed;
        double averageDays = completed == 0 ? 0 : totalDays / completed;
        return new AppealStatisticsDto(total, open, completed, revenue,
            Math.Round(averageCost, 2), Math.Round(averageDays, 1));
    }
}
