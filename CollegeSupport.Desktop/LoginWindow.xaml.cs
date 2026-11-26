using System.Net.Http;
using System.Windows;
using CollegeSupport.Core.Logging;
using CollegeSupport.Desktop.Services;

namespace CollegeSupport.Desktop;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        LoginBox.Focus();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        string login = LoginBox.Text.Trim();
        string password = PasswordInput.Password;

        if (login.Length == 0 || password.Length == 0)
        {
            ShowError("Введите логин и пароль.");
            return;
        }

        LoginButton.IsEnabled = false;
        try
        {
            var result = await AppServices.Api.LoginAsync(login, password);
            AppServices.Api.SetToken(result.Token);
            AppServices.CurrentUser = result.User;
            AppServices.Logger.Info($"Вход выполнен: {result.User.Login} ({result.User.Role})");

            var main = new MainWindow();
            Application.Current.MainWindow = main;
            main.Show();
            Close();
        }
        catch (ApiException ex)
        {
            AppServices.Logger.Warning($"Неудачная попытка входа: {login}");
            ShowError(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            AppServices.Logger.Error("Сервер недоступен при входе", ex);
            ShowError("Сервер недоступен. Запустите CollegeSupport.Api и повторите вход.");
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
