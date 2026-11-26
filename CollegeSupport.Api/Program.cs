using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using CollegeSupport.Api.Infrastructure;
using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Exchange;
using CollegeSupport.Core.Interfaces;
using CollegeSupport.Core.Security;
using CollegeSupport.Core.Services;
using CollegeSupport.Data;
using CollegeSupport.Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

// ---------- Журнал (работа 15) ----------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.File("logs/api-.log",
        formatProvider: CultureInfo.InvariantCulture,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ---------- База данных (работа 9) ----------
    string connectionString = builder.Configuration.GetConnectionString("Default")
                              ?? "Data Source=collegesupport.db";
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

    // ---------- Модули: репозитории и сервисы (работы 8–10, 12) ----------
    builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
    builder.Services.AddScoped<IAppealRepository, AppealRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
    builder.Services.AddScoped<EmployeeService>();
    builder.Services.AddScoped<AppealService>();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<ExchangeService>();

    // ---------- Аутентификация и авторизация (работа 14) ----------
    var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
              ?? throw new InvalidOperationException("В конфигурации нет раздела Jwt.");
    var localDemo = builder.Configuration.GetValue<bool>("LocalDemo");
    if (localDemo && string.IsNullOrEmpty(jwt.Key))
        jwt.Key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    if (jwt.Key.Length < 32)
        throw new InvalidOperationException("Ключ Jwt:Key должен содержать не менее 32 символов.");
    builder.Services.AddSingleton(jwt);
    builder.Services.AddSingleton<ITokenService, JwtTokenService>();

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = jwt.GetSigningKey(),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });
    builder.Services.AddAuthorization();

    // ---------- Ограничение частоты попыток входа (работа 28) ----------
    int loginLimit = builder.Configuration.GetValue("Security:LoginAttemptsPerMinute", 5);
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("login", limiter =>
        {
            limiter.PermitLimit = loginLimit;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
        });
    });

    // ---------- Web API, Swagger, обработка исключений (работы 10, 11) ----------
    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ---------- Проверка работоспособности (работа 19) ----------
    builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

    var app = builder.Build();

    // Применение миграций и начальные данные
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        DbInitializer.Seed(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
    }

    // ---------- Конвейер обработки запросов ----------
    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (!localDemo)
        app.UseHttpsRedirection();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    // ---------- Структурные записи важных бизнес-событий (работа 15) ----------
    Log.Information("CollegeSupport.Api запущен");
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "CollegeSupport.Api аварийно остановлен");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}

// Нужен для интеграционных тестов (WebApplicationFactory<Program>)
public partial class Program { }
