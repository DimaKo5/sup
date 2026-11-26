using System.Text;
using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Entities;
using CollegeSupport.Core.Exceptions;
using CollegeSupport.Core.Interfaces;

namespace CollegeSupport.Core.Exchange;

public record ExportFile(byte[] Content, string ContentType, string FileName);

public class ExchangeService
{
    private readonly IEmployeeRepository _employees;
    private readonly IAppealRepository _appeals;

    public ExchangeService(IEmployeeRepository employees, IAppealRepository appeals)
    {
        _employees = employees;
        _appeals = appeals;
    }

    public async Task<ImportResultDto> ImportEmployeesAsync(string csv)
    {
        var existingPhones = await _employees.GetAllPhonesAsync();
        var (employees, errors) = EmployeeCsvImporter.Parse(csv, existingPhones);
        if (employees.Count > 0)
            await _employees.AddRangeAsync(employees);
        return new ImportResultDto(employees.Count, errors.Count, errors);
    }

    public async Task<ExportFile> ExportAppealsAsync(string format)
    {
        var appeals = (await _appeals.GetAllAsync(new AppealFilter()))
            .Select(a => a.ToDto())
            .ToList();
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmm");

        return format.ToLowerInvariant() switch
        {
            "csv" => new ExportFile(
                Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(AppealExporter.ToCsv(appeals))).ToArray(),
                "text/csv", $"appeals-{stamp}.csv"),
            "json" => new ExportFile(Encoding.UTF8.GetBytes(AppealExporter.ToJson(appeals)),
                "application/json", $"appeals-{stamp}.json"),
            "xml" => new ExportFile(Encoding.UTF8.GetBytes(AppealExporter.ToXml(appeals)),
                "application/xml", $"appeals-{stamp}.xml"),
            _ => throw new BusinessRuleException("Поддерживаемые форматы выгрузки: csv, json, xml.")
        };
    }
}
