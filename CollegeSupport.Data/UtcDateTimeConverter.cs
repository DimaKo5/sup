using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CollegeSupport.Data;

/// <summary>SQLite не хранит часовой пояс, поэтому при чтении явно помечаем дату как UTC.</summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
