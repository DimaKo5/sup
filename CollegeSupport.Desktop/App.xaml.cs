using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using CollegeSupport.Core.Logging;

namespace CollegeSupport.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        AppServices.Logger.Info($"Запуск CollegeSupport.Desktop, сервер: {AppServices.ApiBaseUrl}");
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppServices.Logger.Info("Завершение работы приложения");
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppServices.Logger.Error("Необработанное исключение в потоке интерфейса", e.Exception);
        MessageBox.Show("Произошла непредвиденная ошибка. Подробности записаны в журнал.",
            "CollegeSupport", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e) =>
        AppServices.Logger.Error("Критическая ошибка, приложение будет закрыто", e.ExceptionObject as Exception);

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        AppServices.Logger.Error("Необработанное исключение в фоновой задаче", e.Exception);
        e.SetObserved();
    }
}
