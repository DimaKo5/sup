using System.Windows;
using CollegeSupport.Core.Contracts;
using CollegeSupport.Desktop.Services;

namespace CollegeSupport.Desktop;

public partial class EmployeeEditWindow : Window
{
    public EmployeeDto? CreatedEmployee { get; private set; }

    public EmployeeEditWindow()
    {
        InitializeComponent();
        FullNameBox.Focus();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (FullNameBox.Text.Trim().Length < 3 || string.IsNullOrWhiteSpace(PhoneBox.Text)
            || string.IsNullOrWhiteSpace(CabinetBox.Text))
        {
            Ui.ShowWarning(this, "Заполните ФИО (не менее 3 символов), телефон и кабинет.");
            return;
        }

        string? email = string.IsNullOrWhiteSpace(EmailBox.Text) ? null : EmailBox.Text.Trim();
        var dto = new CreateEmployeeDto(
            FullNameBox.Text.Trim(), PhoneBox.Text.Trim(), email, CabinetBox.Text.Trim());

        bool ok = await Ui.RunSafeAsync(this, async () => CreatedEmployee = await AppServices.Api.CreateEmployeeAsync(dto));
        if (ok)
            DialogResult = true;
    }
}
