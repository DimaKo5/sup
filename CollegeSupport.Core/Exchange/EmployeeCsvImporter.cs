using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Exceptions;
using CollegeSupport.Core.Services;

namespace CollegeSupport.Core.Exchange;

/// <summary>
/// Импорт сотрудников из CSV (разделитель «;», кодировка UTF-8).
/// Сопоставление: ФИО → Employee.FullName, Телефон → Employee.Phone (нормализуется), Email → Employee.Email, Кабинет → Employee.Cabinet.
/// </summary>
public static class EmployeeCsvImporter
{
    public const string Header = "ФИО;Телефон;Email;Кабинет";

    public static (List<Employee> Employees, List<string> Errors) Parse(string csv, ISet<string> existingPhones)
    {
        var employees = new List<Employee>();
        var errors = new List<string>();
        string[] lines = csv.Replace("\r\n", "\n").Split('\n');

        if (!IsHeader(lines[0]))
        {
            errors.Add($"Первая строка файла должна быть заголовком: {Header}");
            return (employees, errors);
        }

        var knownPhones = new HashSet<string>(existingPhones);
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0)
                continue;

            int lineNumber = i + 1;
            string[] cells = line.Split(';');
            if (cells.Length < 3)
            {
                errors.Add($"Строка {lineNumber}: ожидается не менее 3 полей, получено {cells.Length}.");
                continue;
            }

            string fullName = cells[0].Trim();
            if (fullName.Length < 3)
            {
                errors.Add($"Строка {lineNumber}: ФИО должно содержать не менее 3 символов.");
                continue;
            }

            string phone;
            try
            {
                phone = PhoneNormalizer.Normalize(cells[1]);
            }
            catch (BusinessRuleException ex)
            {
                errors.Add($"Строка {lineNumber}: {ex.Message}");
                continue;
            }

            string? email = !string.IsNullOrWhiteSpace(cells[2]) ? cells[2].Trim() : null;
            string cabinet = cells.Length > 3 ? cells[3].Trim() : string.Empty;
            if (cabinet.Length == 0)
            {
                errors.Add($"Строка {lineNumber}: не указан кабинет.");
                continue;
            }

            if (!knownPhones.Add(phone))
            {
                errors.Add($"Строка {lineNumber}: телефон {phone} уже есть в базе или повторяется в файле.");
                continue;
            }

            employees.Add(new Employee { FullName = fullName, Phone = phone, Email = email, Cabinet = cabinet });
        }

        return (employees, errors);
    }

    private static bool IsHeader(string line)
    {
        string[] cells = line.Trim().TrimStart('\uFEFF').Split(';');
        return cells.Length >= 2
               && cells[0].Trim().Equals("ФИО", StringComparison.OrdinalIgnoreCase)
               && cells[1].Trim().Equals("Телефон", StringComparison.OrdinalIgnoreCase);
    }
}
