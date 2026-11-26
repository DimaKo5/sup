using System.IO;
using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Logging;
using CollegeSupport.Desktop.Services;

namespace CollegeSupport.Desktop;

/// <summary>Общие для всех окон объекты: клиент API, текущий пользователь и журнал.</summary>
public static class AppServices
{
    /// <summary>Адрес сервера можно переопределить переменной окружения COLLEGESUPPORT_API_URL.</summary>
    public static string ApiBaseUrl { get; } =
        Environment.GetEnvironmentVariable("COLLEGESUPPORT_API_URL") ?? "http://localhost:5006/";

    public static ApiClient Api { get; } = new(ApiBaseUrl);

    /// <summary>Заполняется после входа (работа 14).</summary>
    public static UserDto? CurrentUser { get; set; }

    /// <summary>Сбрасывает токен и пользователя — возврат к окну входа.</summary>
    public static void Logout()
    {
        Api.SetToken(null);
        CurrentUser = null;
        Logger.Info("Выход пользователя из системы");
    }

    /// <summary>Журнал пишется в %LOCALAPPDATA%\CollegeSupport\logs — туда у пользователя всегда есть права записи.</summary>
    public static IAppLogger Logger { get; } = new FileAppLogger(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CollegeSupport", "logs"),
        AppLogLevel.Info);
}
