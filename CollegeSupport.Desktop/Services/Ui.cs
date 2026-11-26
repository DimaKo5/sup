using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CollegeSupport.Core.Logging;

namespace CollegeSupport.Desktop.Services;

/// <summary>Общие действия интерфейса: безопасный вызов сервера, сообщения, чтение файлов.</summary>
public static class Ui
{
    /// <summary>Выполняет обращение к серверу и превращает исключения в понятные сообщения.</summary>
    public static async Task<bool> RunSafeAsync(Window owner, Func<Task> action)
    {
        owner.Cursor = Cursors.Wait;
        try
        {
            await action();
            return true;
        }
        catch (ApiException ex)
        {
            AppServices.Logger.Warning($"Ошибка API {(int)ex.StatusCode}: {ex.Message}");
            ShowError(owner, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            AppServices.Logger.Error("Сервер недоступен", ex);
            ShowError(owner, "Не удалось подключиться к серверу. Проверьте, что CollegeSupport.Api запущен.");
        }
        catch (TaskCanceledException ex)
        {
            AppServices.Logger.Error("Сервер не ответил вовремя", ex);
            ShowError(owner, "Сервер не ответил за 15 секунд. Повторите попытку позже.");
        }
        finally
        {
            owner.Cursor = null;
        }
        return false;
    }

    public static void ShowError(Window owner, string message) =>
        MessageBox.Show(owner, message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

    public static void ShowWarning(Window owner, string message) =>
        MessageBox.Show(owner, message, "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);

    public static void ShowInfo(Window owner, string message) =>
        MessageBox.Show(owner, message, "CollegeSupport", MessageBoxButton.OK, MessageBoxImage.Information);

    /// <summary>Читает CSV в UTF-8, а если файл сохранён Excel в кодировке Windows-1251 — в ней.</summary>
    public static string ReadCsvText(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        try
        {
            return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes).TrimStart('\uFEFF');
        }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1251).GetString(bytes);
        }
    }
}
