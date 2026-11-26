using CollegeSupport.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace CollegeSupport.Tests.Api;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"cs-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting имеет приоритет над appsettings.json и гарантирует отдельную БД для каждого теста.
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath};Pooling=False");
        builder.UseSetting("Jwt:Key", "test-only-key-change-me-0123456789-abcdefghijklmnop");
        builder.UseSetting("Jwt:Issuer", "CollegeSupport.Api");
        builder.UseSetting("Jwt:Audience", "CollegeSupport.Clients");
        builder.UseSetting("Jwt:ExpiresMinutes", "480");
        builder.UseSetting("Security:LoginAttemptsPerMinute", "1000");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }
}
