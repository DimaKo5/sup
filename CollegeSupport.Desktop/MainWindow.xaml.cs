using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Logging;
using CollegeSupport.Desktop.Services;
using Microsoft.Win32;

namespace CollegeSupport.Desktop;

public partial class MainWindow : Window
{
    /// <summary>Элемент выпадающего списка статусов: русское название + значение (null = «все»).</summary>
    private sealed record StatusItem(string Name, AppealStatus? Value);
    private sealed record RoleItem(string Name, UserRole Role);
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private List<EmployeeDto> _employees = [];
    private int _appealsRequest;

    public MainWindow()
    {
        InitializeComponent();

        var filterItems = new List<StatusItem> { new("Все статусы", null) };
        filterItems.AddRange(Enum.GetValues<AppealStatus>().Select(s => new StatusItem(s.ToDisplayName(), s)));
        StatusFilter.ItemsSource = filterItems;
        StatusFilter.SelectedIndex = 0;

        StatusFilter.SelectionChanged += async (_, _) => await LoadAppealsAsync();
        SearchBox.TextChanged += (_, _) =>
        {
            _searchTimer.Stop();
            ++_appealsRequest;
            _searchTimer.Start();
        };
        _searchTimer.Tick += async (_, _) => await LoadAppealsAsync();
        Closed += (_, _) =>
        {
            _searchTimer.Stop();
            ++_appealsRequest;
        };

        UserRoleBox.ItemsSource = Enum.GetValues<UserRole>()
            .Select(role => new RoleItem(role.ToDisplayName(), role)).ToList();
        UserRoleBox.SelectedItem = ((IEnumerable<RoleItem>)UserRoleBox.ItemsSource)
            .First(item => item.Role == UserRole.Technician);
        WorkspaceTabs.SelectionChanged += WorkspaceTabs_SelectionChanged;
    }

    private AppealDto? SelectedAppeal => AppealsGrid.SelectedItem as AppealDto;

    private bool IsTechnician => AppServices.CurrentUser?.Role == UserRole.Technician;

    /// <summary>Возврат к окну авторизации: токен сброшен, главное окно заменено на LoginWindow.</summary>
    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        string login = AppServices.CurrentUser?.Login ?? "неизвестный";
        AppServices.Logout();
        AppServices.Logger.Info($"Пользователь {login} нажал «Выйти»");

        var loginWindow = new LoginWindow();
        Application.Current.MainWindow = loginWindow;
        loginWindow.Show();
        Close();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var user = AppServices.CurrentUser;
        UserText.Text = user is null ? string.Empty : $"{user.Role.ToDisplayName()} · {user.Login}";
        ApplyRolePermissions();

        if (!IsTechnician)
            await Ui.RunSafeAsync(this, async () => TechnicianBox.ItemsSource = await AppServices.Api.GetTechniciansAsync());

        await LoadAppealsAsync();
    }

    /// <summary>Техник видит обращения и меняет статус, но не создаёт обращения и не назначает техников.</summary>
    private void ApplyRolePermissions()
    {
        var visibility = IsTechnician ? Visibility.Collapsed : Visibility.Visible;
        NewAppealButton.Visibility = visibility;
        TechnicianGroup.Visibility = visibility;
        ExchangeGroup.Visibility = visibility;
        EmployeesTab.Visibility = visibility;
        UsersTab.Visibility = AppServices.CurrentUser?.Role == UserRole.Admin ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task LoadAppealsAsync()
    {
        _searchTimer.Stop();
        int request = ++_appealsRequest;
        var status = (StatusFilter.SelectedItem as StatusItem)?.Value;
        int? selectedId = SelectedAppeal?.Id;

        await Ui.RunSafeAsync(this, async () =>
        {
            var appeals = await AppServices.Api.GetAppealsAsync(status, SearchBox.Text);
            if (request != _appealsRequest)
                return;

            AppealsGrid.ItemsSource = appeals;
            CountText.Text = appeals.Count == 0 ? "По выбранным условиям обращений нет." : $"Найдено обращений: {appeals.Count}";
            if (selectedId.HasValue)
                AppealsGrid.SelectedItem = appeals.FirstOrDefault(a => a.Id == selectedId);
        });
    }

    private async void FindButton_Click(object sender, RoutedEventArgs e) => await LoadAppealsAsync();

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await LoadAppealsAsync();
    }

    private void AppealsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var appeal = SelectedAppeal;
        NextStatusBox.ItemsSource = appeal is null
            ? null
            : AppealStatusRules.NextStatuses(appeal.Status)
                .Select(s => new StatusItem(s.ToDisplayName(), s))
                .ToList();
        NextStatusBox.SelectedIndex = 0;
        CostBox.Text = appeal?.Cost?.ToString("0.00", CultureInfo.CurrentCulture) ?? string.Empty;
        TechnicianBox.SelectedItem = (TechnicianBox.ItemsSource as List<UserDto>)?.FirstOrDefault(u => u.Id == appeal?.TechnicianId);
    }

    private async void NewAppealButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AppealEditWindow { Owner = this };
        if (dialog.ShowDialog() == true)
            await LoadAppealsAsync();
    }

    private async void ChangeStatusButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedAppeal is not { } appeal)
        {
            Ui.ShowWarning(this, "Выберите обращение в таблице.");
            return;
        }
        if (NextStatusBox.SelectedItem is not StatusItem { Value: { } newStatus })
        {
            Ui.ShowWarning(this, "Для обращения в этом статусе нет доступных переходов.");
            return;
        }

        decimal? cost = null;
        if (!string.IsNullOrWhiteSpace(CostBox.Text))
        {
            if (!TryParseMoney(CostBox.Text, out decimal parsed))
            {
                Ui.ShowWarning(this, "Стоимость должна быть числом, например 1500 или 1500,50.");
                return;
            }
            cost = parsed;
        }

        bool ok = await Ui.RunSafeAsync(this, () => AppServices.Api.ChangeStatusAsync(appeal.Id, newStatus, cost));
        if (ok)
        {
            AppServices.Logger.Info($"Обращение {appeal.Id}: статус → {newStatus}");
            if ((StatusFilter.SelectedItem as StatusItem)?.Value is { } activeFilter && activeFilter != newStatus)
                StatusFilter.SelectedItem = ((IEnumerable<StatusItem>)StatusFilter.ItemsSource)
                    .First(item => item.Value == newStatus);
            else
                await LoadAppealsAsync();
        }
    }

    private async void WorkspaceTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, WorkspaceTabs))
            return;

        if (WorkspaceTabs.SelectedItem == EmployeesTab && !IsTechnician)
            await LoadEmployeesAsync();
        else if (WorkspaceTabs.SelectedItem == UsersTab && AppServices.CurrentUser?.Role == UserRole.Admin)
            await LoadUsersAsync();
    }

    private Task LoadEmployeesAsync() => Ui.RunSafeAsync(this, async () =>
    {
        _employees = await AppServices.Api.GetEmployeesAsync();
        FilterEmployees();
    });

    private void EmployeeSearchBox_TextChanged(object sender, TextChangedEventArgs e) => FilterEmployees();

    private void FilterEmployees()
    {
        if (EmployeesGrid is null)
            return;

        string search = EmployeeSearchBox.Text.Trim();
        EmployeesGrid.ItemsSource = _employees.Where(employee =>
            employee.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
            || employee.Phone.Contains(search, StringComparison.OrdinalIgnoreCase)
            || employee.Cabinet.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async void AddEmployeeButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new EmployeeEditWindow { Owner = this };
        if (dialog.ShowDialog() == true)
            await LoadEmployeesAsync();
    }

    private Task LoadUsersAsync() => Ui.RunSafeAsync(this, async () =>
    {
        var users = await AppServices.Api.GetUsersAsync();
        UsersGrid.ItemsSource = users.Select(user =>
            new { user.FullName, user.Login, RoleName = user.Role.ToDisplayName() }).ToList();
    });

    private async void AddUserButton_Click(object sender, RoutedEventArgs e)
    {
        if (UserRoleBox.SelectedItem is not RoleItem role)
            return;

        var dto = new CreateUserDto(UserLoginBox.Text.Trim(), UserPasswordBox.Password,
            UserNameBox.Text.Trim(), role.Role);
        AddUserButton.IsEnabled = false;
        try
        {
            if (!await Ui.RunSafeAsync(this, () => AppServices.Api.CreateUserAsync(dto)))
                return;

            UserNameBox.Clear();
            UserLoginBox.Clear();
            UserPasswordBox.Clear();
            await LoadUsersAsync();
            await Ui.RunSafeAsync(this, async () => TechnicianBox.ItemsSource = await AppServices.Api.GetTechniciansAsync());
        }
        finally
        {
            AddUserButton.IsEnabled = true;
        }
    }

    private async void AssignButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedAppeal is not { } appeal || TechnicianBox.SelectedItem is not UserDto technician)
        {
            Ui.ShowWarning(this, "Выберите обращение и техника.");
            return;
        }

        bool ok = await Ui.RunSafeAsync(this, () => AppServices.Api.AssignTechnicianAsync(appeal.Id, technician.Id));
        if (ok)
            await LoadAppealsAsync();
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Файлы CSV (*.csv)|*.csv", Title = "Импорт сотрудников" };
        if (dialog.ShowDialog(this) != true)
            return;

        await Ui.RunSafeAsync(this, async () =>
        {
            string csv = Ui.ReadCsvText(dialog.FileName);
            var result = await AppServices.Api.ImportEmployeesAsync(csv);
            AppServices.Logger.Info($"Импорт сотрудников из {Path.GetFileName(dialog.FileName)}: {result.Imported} / {result.Skipped}");
            string text = $"Импортировано: {result.Imported}, пропущено: {result.Skipped}.";
            if (result.Errors.Count > 0)
                text += Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, result.Errors.Take(15));
            Ui.ShowInfo(this, text);
        });
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Выгрузка обращений",
            Filter = "CSV для Excel (*.csv)|*.csv|JSON (*.json)|*.json|XML (*.xml)|*.xml",
            FileName = $"appeals-{DateTime.Now:yyyyMMdd}"
        };
        if (dialog.ShowDialog(this) != true)
            return;

        string format = dialog.FilterIndex switch { 2 => "json", 3 => "xml", _ => "csv" };
        await Ui.RunSafeAsync(this, async () =>
        {
            byte[] content = await AppServices.Api.ExportAppealsAsync(format);
            await File.WriteAllBytesAsync(dialog.FileName, content);
            Ui.ShowInfo(this, $"Файл сохранён: {dialog.FileName}");
        });
    }

    private async void StatisticsButton_Click(object sender, RoutedEventArgs e)
    {
        int? technicianId = IsTechnician ? AppServices.CurrentUser!.Id : null;
        await Ui.RunSafeAsync(this, async () =>
        {
            var s = await AppServices.Api.GetStatisticsAsync(technicianId);
            Ui.ShowInfo(this,
                $"Всего обращений: {s.Total}\n" +
                $"Открыто: {s.Open}\n" +
                $"Завершено: {s.Completed}\n" +
                $"Сумма работ: {s.Revenue:N2} руб.\n" +
                $"Средняя стоимость: {s.AverageCost:N2} руб.\n" +
                $"Средний срок решения: {s.AverageResolveDays:N1} дн.");
        });
    }

    /// <summary>Принимает и «1500,50», и «1500.50» независимо от региональных настроек.</summary>
    private static bool TryParseMoney(string text, out decimal value) =>
        decimal.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value)
        && value >= 0;
}
