using System.Diagnostics;
using System.Runtime.InteropServices;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using Application = FlaUI.Core.Application;

namespace CollegeSupport.UiTests;

public sealed class InteractiveFactAttribute : FactAttribute
{
    public InteractiveFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COLLEGESUPPORT_UI_EXE")))
            Skip = "Укажите путь к клиенту в COLLEGESUPPORT_UI_EXE и запустите API в интерактивном сеансе.";
    }
}

public class LoginUiTests
{
    private const string LoginTitle = "Вход — CollegeSupport";
    private const string MainTitle = "CollegeSupport — обращения технической поддержки";
    private sealed class TestClient : IDisposable
    {
        public Application Application { get; } =
            Application.Launch(Environment.GetEnvironmentVariable("COLLEGESUPPORT_UI_EXE")!);

        public void Dispose()
        {
            if (!Application.HasExited)
                Application.Close(killIfCloseFails: true);
            Application.Dispose();
        }
    }

    [InteractiveFact]
    public void LoginWindow_OpensAndShowsTitle()
    {
        using var automation = new UIA3Automation();
        using var client = new TestClient();
        var app = client.Application;
        Assert.Equal(LoginTitle, WaitWindow(app, automation, LoginTitle).Title);
    }

    [InteractiveFact]
    public void Login_WithEmptyFields_ShowsError()
    {
        using var automation = new UIA3Automation();
        using var client = new TestClient();
        var app = client.Application;
        var window = WaitWindow(app, automation, LoginTitle);
        Control(window, "LoginButton").AsButton().Invoke();
        Assert.Equal("Введите логин и пароль.",
            WaitFor(() => window.FindFirstDescendant(cf => cf.ByAutomationId("ErrorText"))).Name);
    }

    [InteractiveFact]
    public void Login_WithValidCredentials_OpensMainWindow()
    {
        using var automation = new UIA3Automation();
        using var client = new TestClient();
        var app = client.Application;
        Assert.Equal(MainTitle, SignIn(app, automation).Title);
    }

    [InteractiveFact]
    public void Logout_ReturnsToLoginWindow()
    {
        using var automation = new UIA3Automation();
        using var client = new TestClient();
        var app = client.Application;
        var main = SignIn(app, automation);
        Control(main, "LogoutButton").AsButton().Invoke();
        Assert.Equal(LoginTitle, WaitWindow(app, automation, LoginTitle).Title);
    }

    [InteractiveFact]
    public void MainWindow_HasAppealsGrid()
    {
        using var automation = new UIA3Automation();
        using var client = new TestClient();
        var app = client.Application;
        var main = SignIn(app, automation);
        Assert.NotEmpty(Control(main, "AppealsGrid").AsDataGridView().Rows);
    }

    [InteractiveFact]
    public void MainWindow_SearchWithoutMatchShowsZeroRows()
    {
        using var automation = new UIA3Automation();
        using var client = new TestClient();
        var app = client.Application;
        var main = SignIn(app, automation);
        var grid = Control(main, "AppealsGrid").AsDataGridView();
        Assert.NotEmpty(grid.Rows);
        Control(main, "SearchBox").AsTextBox().Text = "NoMatch-" + Guid.NewGuid().ToString("N");
        Control(main, "FindButton").AsButton().Invoke();
        Assert.True(Retry.WhileFalse(() => grid.Rows.Length == 0, TimeSpan.FromSeconds(10)).Success);
    }

    private static Window SignIn(Application app, UIA3Automation automation)
    {
        var login = WaitWindow(app, automation, LoginTitle);
        Control(login, "LoginBox").AsTextBox().Text = "dispatcher";
        Control(login, "PasswordInput").AsTextBox().Enter("Dispatcher123");
        Control(login, "LoginButton").AsButton().Invoke();
        var main = WaitWindow(app, automation, MainTitle);
        Assert.True(Retry.WhileFalse(
            () => main.FindFirstDescendant(cf => cf.ByAutomationId("AppealsGrid"))?.AsDataGridView().Rows.Length > 0,
            TimeSpan.FromSeconds(15)).Success);
        return main;
    }

    private static Window WaitWindow(Application app, UIA3Automation automation, string title)
    {
        using var process = Process.GetProcessById(app.ProcessId);
        return WaitFor<Window>(() =>
        {
            process.Refresh();
            if (process.HasExited || process.MainWindowTitle != title || process.MainWindowHandle == IntPtr.Zero)
                return null;

            try
            {
                return automation.FromHandle(process.MainWindowHandle).AsWindow();
            }
            catch (COMException)
            {
                return null;
            }
        });
    }

    private static AutomationElement Control(Window window, string automationId) =>
        WaitFor(() => window.FindFirstDescendant(cf => cf.ByAutomationId(automationId)));

    private static T WaitFor<T>(Func<T?> read) where T : class =>
        Retry.WhileNull(read, TimeSpan.FromSeconds(15)).Result
        ?? throw new InvalidOperationException("Элемент интерфейса не найден.");
}
