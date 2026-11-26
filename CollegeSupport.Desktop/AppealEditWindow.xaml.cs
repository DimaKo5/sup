using System.Windows;
using CollegeSupport.Core.Contracts;
using CollegeSupport.Desktop.Services;

namespace CollegeSupport.Desktop;

public partial class AppealEditWindow : Window
{
    public AppealEditWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await LoadEmployeesAsync();

    private async Task LoadEmployeesAsync(int? selectEmployeeId = null)
    {
        await Ui.RunSafeAsync(this, async () =>
        {
            var employees = await AppServices.Api.GetEmployeesAsync();
            EmployeeBox.ItemsSource = employees;
            if (selectEmployeeId.HasValue)
                EmployeeBox.SelectedItem = employees.FirstOrDefault(x => x.Id == selectEmployeeId);
        });
    }

    private async void NewEmployeeButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new EmployeeEditWindow { Owner = this };
        if (dialog.ShowDialog() == true && dialog.CreatedEmployee is { } employee)
            await LoadEmployeesAsync(employee.Id);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        string? error = Validate();
        if (error is not null)
        {
            ShowError(error);
            return;
        }

        var dto = new CreateAppealDto(
            ((EmployeeDto)EmployeeBox.SelectedItem).Id,
            CabinetBox.Text.Trim(),
            EquipmentBox.Text.Trim(),
            ProblemBox.Text.Trim());

        SaveButton.IsEnabled = false;
        bool ok = await Ui.RunSafeAsync(this, () => AppServices.Api.CreateAppealAsync(dto));
        SaveButton.IsEnabled = true;
        if (ok)
            DialogResult = true;
    }

    /// <summary>Проверка на клиенте: быстрый отклик без обращения к серверу. Сервер всё равно проверит данные повторно.</summary>
    private string? Validate()
    {
        if (EmployeeBox.SelectedItem is not EmployeeDto)
            return "Выберите сотрудника из списка или добавьте нового.";
        if (string.IsNullOrWhiteSpace(CabinetBox.Text))
            return "Укажите кабинет.";
        if (string.IsNullOrWhiteSpace(EquipmentBox.Text))
            return "Укажите оборудование.";
        if (ProblemBox.Text.Trim().Length < 10)
            return "Описание проблемы должно содержать не менее 10 символов.";
        return null;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
