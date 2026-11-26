using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Xml.Serialization;
using CollegeSupport.Core.Contracts;

namespace CollegeSupport.Core.Exchange;

/// <summary>Модель обращения во внешнем формате обмена (приёмник данных).</summary>
public class AppealExportItem
{
    [XmlAttribute("id")]
    public int Id { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string Employee { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Cabinet { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Technician { get; set; }
    public decimal? Cost { get; set; }

    public static AppealExportItem FromDto(AppealDto a) => new()
    {
        Id = a.Id,
        CreatedAt = a.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
        Employee = a.EmployeeName,
        Phone = a.EmployeePhone,
        Cabinet = a.Cabinet,
        Equipment = a.Equipment,
        Status = a.StatusName,
        Technician = a.TechnicianName,
        Cost = a.Cost
    };
}

[XmlRoot("Appeals")]
public class AppealExportList
{
    [XmlElement("Appeal")]
    public List<AppealExportItem> Items { get; set; } = new();
}

public static class AppealExporter
{
    public const string CsvHeader = "Номер;Дата;Сотрудник;Телефон;Кабинет;Оборудование;Статус;Техник;Стоимость";

    public static string ToCsv(IEnumerable<AppealDto> appeals)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CsvHeader);
        foreach (var item in appeals.Select(AppealExportItem.FromDto))
        {
            sb.Append(item.Id).Append(';')
              .Append(item.CreatedAt).Append(';')
              .Append(Escape(item.Employee)).Append(';')
              .Append(item.Phone).Append(';')
              .Append(Escape(item.Cabinet)).Append(';')
              .Append(Escape(item.Equipment)).Append(';')
              .Append(item.Status).Append(';')
              .Append(Escape(item.Technician)).Append(';')
              .Append(item.Cost?.ToString("0.00", CultureInfo.InvariantCulture))
              .AppendLine();
        }
        return sb.ToString();
    }

    public static string ToJson(IEnumerable<AppealDto> appeals)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        return JsonSerializer.Serialize(appeals.Select(AppealExportItem.FromDto), options);
    }

    public static string ToXml(IEnumerable<AppealDto> appeals)
    {
        var list = new AppealExportList { Items = appeals.Select(AppealExportItem.FromDto).ToList() };
        var serializer = new XmlSerializer(typeof(AppealExportList));
        using var writer = new Utf8StringWriter();
        serializer.Serialize(writer, list);
        return writer.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        if (value.IndexOfAny([';', '"', '\n', '\r']) < 0)
            return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
