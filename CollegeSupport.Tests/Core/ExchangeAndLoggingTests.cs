using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Exchange;
using CollegeSupport.Core.Logging;
using CollegeSupport.Core.Security;
using CollegeSupport.Core.Services;

namespace CollegeSupport.Tests.Core;

public class ExchangeAndLoggingTests
{
    [Fact]
    public void EmployeeCsvImporter_ParsesValidRows()
    {
        string csv = "ФИО;Телефон;Email;Кабинет\nПавлов Павел;8 900 000-00-01;p@example.com;305\n";
        var (employees, errors) = EmployeeCsvImporter.Parse(csv, new HashSet<string>());
        Assert.Single(employees);
        Assert.Empty(errors);
        Assert.Equal("+79000000001", employees[0].Phone);
        Assert.Equal("305", employees[0].Cabinet);
    }

    [Fact]
    public void EmployeeCsvImporter_ReportsBadPhoneAndDuplicate()
    {
        string csv = "ФИО;Телефон;Email;Кабинет\nПавлов Павел;123;;305\nИванов Иван;89001112233;;101\n";
        var (_, errors) = EmployeeCsvImporter.Parse(csv, new HashSet<string> { "+79001112233" });
        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void EmployeeCsvImporter_RejectsWrongHeader()
    {
        var (_, errors) = EmployeeCsvImporter.Parse("Имя;Телефон\nИванов;9001234567", new HashSet<string>());
        Assert.Single(errors);
    }

    [Theory]
    [InlineData("a@b;c", "\"a@b;c\"")]
    [InlineData("просто", "просто")]
    [InlineData("a\"b", "\"a\"\"b\"")]
    public void Exporter_EscapesCsvValues(string value, string expected)
    {
        var dto = new AppealDto(1, DateTime.UtcNow, 1, value, "+79001112233", "101",
            "Ноутбук", "Описание проблемы", AppealStatus.New, "Новое", null, null, null, null);
        string csv = AppealExporter.ToCsv([dto]);
        Assert.Contains(expected, csv);
    }

    [Fact]
    public void Exporter_ProducesJsonAndXml()
    {
        var dto = new AppealDto(1, DateTime.UtcNow, 1, "Иванов", "+79001112233", "101",
            "Ноутбук", "Описание проблемы", AppealStatus.New, "Новое", null, null, null, null);
        Assert.Contains("Иванов", AppealExporter.ToJson([dto]));
        Assert.Contains("<Appeals", AppealExporter.ToXml([dto]));
    }

    [Fact]
    public void PasswordHasher_VerifiesPassword()
    {
        IPasswordHasher hasher = new Pbkdf2PasswordHasher();
        var (hash, salt) = hasher.HashPassword("Admin123");
        Assert.True(hasher.Verify("Admin123", hash, salt));
        Assert.False(hasher.Verify("Wrong123", hash, salt));
        Assert.NotEqual(hash, hasher.HashPassword("Admin123").Hash);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abcdefgh")]
    [InlineData("12345678")]
    public void PasswordPolicy_RejectsWeakPasswords(string password) =>
        Assert.Throws<CollegeSupport.Core.Exceptions.BusinessRuleException>(() => PasswordPolicy.Validate(password));

    [Fact]
    public void FileAppLogger_FiltersByLevelAndUsesInjectedClock()
    {
        string dir = Path.Combine(Path.GetTempPath(), "cs-logs-" + Guid.NewGuid());
        var clock = new DateTime(2026, 11, 20, 12, 0, 0);
        var logger = new FileAppLogger(dir, AppLogLevel.Warning, () => clock);
        logger.Info("не должно попасть");
        logger.Warning("попало");
        string text = File.ReadAllText(logger.CurrentFilePath);
        Assert.Contains("попало", text);
        Assert.DoesNotContain("не должно попасть", text);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void FileAppLogger_DeletesFilesOlderThan14Days()
    {
        string dir = Path.Combine(Path.GetTempPath(), "cs-logs-" + Guid.NewGuid());
        var now = new DateTime(2026, 11, 20);
        Directory.CreateDirectory(dir);
        string old = Path.Combine(dir, "desktop-2026-01-01.log");
        File.WriteAllText(old, "старый");
        File.SetLastWriteTime(old, new DateTime(2026, 1, 1));
        _ = new FileAppLogger(dir, AppLogLevel.Info, () => now);
        Assert.False(File.Exists(old));
        Directory.Delete(dir, true);
    }

    [Fact]
    public void JsonDefaults_SerializesEnumsAsStrings()
    {
        string json = System.Text.Json.JsonSerializer.Serialize(
            new { Status = AppealStatus.InProgress }, CollegeSupport.Core.Contracts.JsonDefaults.Options);
        Assert.Contains("InProgress", json);
    }
}
