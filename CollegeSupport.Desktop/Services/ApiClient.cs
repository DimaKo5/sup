using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Entities;

namespace CollegeSupport.Desktop.Services;

/// <summary>Ошибка, которую вернул сервер (код HTTP + понятное пользователю сообщение).</summary>
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public ApiException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>Клиент REST API: единственное место, где приложение обращается к серверу.</summary>
public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public void SetToken(string? token) =>
        _http.DefaultRequestHeaders.Authorization =
            token is null ? null : new AuthenticationHeaderValue("Bearer", token);

    // ---------- Вход ----------
    public Task<LoginResponseDto> LoginAsync(string login, string password) =>
        SendAsync<LoginResponseDto>(HttpMethod.Post, "api/auth/login", new LoginRequestDto(login, password));

    // ---------- Обращения ----------
    public Task<List<AppealDto>> GetAppealsAsync(AppealStatus? status, string? search)
    {
        var query = new List<string>();
        if (status.HasValue)
            query.Add("status=" + status.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query.Add("search=" + Uri.EscapeDataString(search.Trim()));
        string url = "api/appeals" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        return SendAsync<List<AppealDto>>(HttpMethod.Get, url);
    }

    public Task<AppealDto> CreateAppealAsync(CreateAppealDto dto) =>
        SendAsync<AppealDto>(HttpMethod.Post, "api/appeals", dto);

    public Task<AppealDto> ChangeStatusAsync(int id, AppealStatus status, decimal? cost) =>
        SendAsync<AppealDto>(HttpMethod.Put, $"api/appeals/{id}/status", new ChangeStatusDto(status, cost));

    public Task<AppealDto> AssignTechnicianAsync(int id, int technicianId) =>
        SendAsync<AppealDto>(HttpMethod.Put, $"api/appeals/{id}/technician", new AssignTechnicianDto(technicianId));

    public Task<AppealStatisticsDto> GetStatisticsAsync(int? technicianId = null) =>
        SendAsync<AppealStatisticsDto>(HttpMethod.Get,
            "api/appeals/statistics" + (technicianId.HasValue ? $"?technicianId={technicianId}" : ""));

    // ---------- Сотрудники и пользователи ----------
    public Task<List<EmployeeDto>> GetEmployeesAsync() =>
        SendAsync<List<EmployeeDto>>(HttpMethod.Get, "api/employees");

    public Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto) =>
        SendAsync<EmployeeDto>(HttpMethod.Post, "api/employees", dto);

    public Task<List<UserDto>> GetTechniciansAsync() =>
        SendAsync<List<UserDto>>(HttpMethod.Get, "api/users/technicians");

    // ---------- Обмен данными ----------
    public Task<ImportResultDto> ImportEmployeesAsync(string csv) =>
        SendAsync<ImportResultDto>(HttpMethod.Post, "api/exchange/employees/import", new ImportCsvDto(csv));

    public async Task<byte[]> ExportAppealsAsync(string format)
    {
        var stopwatch = Stopwatch.StartNew();
        using var response = await _http.GetAsync($"api/exchange/appeals/export?format={format}");
        TraceRequest(HttpMethod.Get, "api/exchange/appeals/export", response.StatusCode, stopwatch.ElapsedMilliseconds);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsByteArrayAsync();
    }

    // ---------- Проверка сервера (работа 19) ----------
    public async Task<bool> IsServerHealthyAsync()
    {
        try
        {
            using var response = await _http.GetAsync("health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    // ---------- Общая часть ----------
    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonDefaults.Options);

        var stopwatch = Stopwatch.StartNew();
        using var response = await _http.SendAsync(request);
        TraceRequest(method, url, response.StatusCode, stopwatch.ElapsedMilliseconds);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(JsonDefaults.Options))!;
    }

    /// <summary>Замер времени запроса: Debug.WriteLine — в Debug, Trace — в обеих конфигурациях (работа 18).</summary>
    private static void TraceRequest(HttpMethod method, string url, HttpStatusCode status, long milliseconds)
    {
        string message = $"{method} {url} → {(int)status} за {milliseconds} мс";
        Debug.WriteLine(message);
        Trace.WriteLine(message);
    }

    /// <summary>Превращает ответ с ошибкой (ProblemDetails) в ApiException с понятным текстом.</summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        string message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Сеанс истёк или не выполнен вход. Войдите в систему заново.",
            HttpStatusCode.Forbidden => "Недостаточно прав для выполнения операции.",
            HttpStatusCode.TooManyRequests => "Слишком много попыток. Подождите минуту и повторите.",
            _ => $"Ошибка сервера ({(int)response.StatusCode})."
        };

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemInfo>(JsonDefaults.Options);
            if (problem?.Errors is { Count: > 0 })
                message = string.Join(Environment.NewLine, problem.Errors.SelectMany(e => e.Value));
            else if (!string.IsNullOrWhiteSpace(problem?.Detail))
                message = problem.Detail;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // тело ответа пустое или не JSON — оставляем сообщение по коду ответа
        }

        throw new ApiException(response.StatusCode, message);
    }

    private sealed class ProblemInfo
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
