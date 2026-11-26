using System.Text;

namespace CollegeSupport.Core.Logging;

public enum AppLogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}

public interface IAppLogger
{
    void Log(AppLogLevel level, string message, Exception? exception = null);
}

public static class AppLoggerExtensions
{
    public static void Debug(this IAppLogger logger, string message) => logger.Log(AppLogLevel.Debug, message);
    public static void Info(this IAppLogger logger, string message) => logger.Log(AppLogLevel.Info, message);
    public static void Warning(this IAppLogger logger, string message, Exception? ex = null) => logger.Log(AppLogLevel.Warning, message, ex);
    public static void Error(this IAppLogger logger, string message, Exception? ex = null) => logger.Log(AppLogLevel.Error, message, ex);
}

/// <summary>Потокобезопасный журнал в текстовых файлах: один файл на день, фильтр по минимальному уровню.</summary>
public class FileAppLogger : IAppLogger
{
    private readonly string _directory;
    private readonly AppLogLevel _minLevel;
    private readonly Func<DateTime> _clock;
    private readonly object _sync = new();

    public FileAppLogger(string directory, AppLogLevel minLevel = AppLogLevel.Info, Func<DateTime>? clock = null)
    {
        _directory = directory;
        _minLevel = minLevel;
        _clock = clock ?? (() => DateTime.Now);
        Directory.CreateDirectory(_directory);
        DeleteOldFiles();
    }

    public string CurrentFilePath => Path.Combine(_directory, $"desktop-{_clock():yyyy-MM-dd}.log");

    public void Log(AppLogLevel level, string message, Exception? exception = null)
    {
        if (level < _minLevel)
            return;

        var line = new StringBuilder()
            .Append(_clock().ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append(" [").Append(LevelCode(level)).Append("] ")
            .Append(message);
        if (exception is not null)
            line.AppendLine().Append(exception);

        lock (_sync)
        {
            File.AppendAllText(CurrentFilePath, line.AppendLine().ToString(), Encoding.UTF8);
        }
    }

    /// <summary>Удаляет при запуске файлы журнала старше 14 дней (работа 15).</summary>
    public void DeleteOldFiles()
    {
        if (!Directory.Exists(_directory))
            return;
        DateTime threshold = _clock().AddDays(-14);
        foreach (string file in Directory.EnumerateFiles(_directory, "desktop-*.log"))
        {
            if (File.GetLastWriteTime(file) < threshold)
                File.Delete(file);
        }
    }

    private static string LevelCode(AppLogLevel level) => level switch
    {
        AppLogLevel.Debug => "DBG",
        AppLogLevel.Info => "INF",
        AppLogLevel.Warning => "WRN",
        AppLogLevel.Error => "ERR",
        _ => "???"
    };
}
