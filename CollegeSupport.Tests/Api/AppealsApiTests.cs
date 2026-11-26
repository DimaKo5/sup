using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CollegeSupport.Core.Contracts;
using CollegeSupport.Core.Entities;

namespace CollegeSupport.Tests.Api;

public class AppealsApiTests : IDisposable
{
    private readonly ApiFactory _factory;

    public AppealsApiTests()
    {
        _factory = new ApiFactory();
    }

    public void Dispose() => _factory.Dispose();

    private static string NewPhone() => "+7" + Random.Shared.Next(1000000000, 2000000000);

    private async Task<HttpClient> AuthorizedClientAsync(string login = "dispatcher", string password = "Dispatcher123")
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequestDto(login, password));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonDefaults.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.Token);
        return client;
    }

    [Fact]
    public async Task GetAppeals_ReturnsSevenSeededAppeals()
    {
        var client = await AuthorizedClientAsync();
        var appeals = await client.GetFromJsonAsync<List<AppealDto>>("api/appeals", JsonDefaults.Options);
        Assert.NotNull(appeals);
        Assert.Equal(7, appeals.Count);
    }

    [Fact]
    public async Task GetAppeals_WithStatusFilter_ReturnsOnlyMatching()
    {
        var client = await AuthorizedClientAsync();
        var appeals = await client.GetFromJsonAsync<List<AppealDto>>("api/appeals?status=InProgress", JsonDefaults.Options);
        Assert.NotNull(appeals);
        Assert.All(appeals, a => Assert.Equal(AppealStatus.InProgress, a.Status));
    }

    [Fact]
    public async Task GetAppeals_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/appeals");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login",
            new LoginRequestDto("dispatcher", "Wrong123"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAppeal_WithMissingId_Returns404()
    {
        var client = await AuthorizedClientAsync();
        var response = await client.GetAsync("api/appeals/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_WithInvalidTransition_Returns400()
    {
        var client = await AuthorizedClientAsync();
        var response = await client.PutAsJsonAsync("api/appeals/6/status",
            new ChangeStatusDto(AppealStatus.Closed, null));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_WithoutCost_Returns400()
    {
        var client = await AuthorizedClientAsync();
        await client.PutAsJsonAsync("api/appeals/6/technician", new AssignTechnicianDto(3));
        await client.PutAsJsonAsync("api/appeals/6/status", new ChangeStatusDto(AppealStatus.InProgress, null));
        var response = await client.PutAsJsonAsync("api/appeals/6/status",
            new ChangeStatusDto(AppealStatus.Resolved, null));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_Returns201()
    {
        var client = await AuthorizedClientAsync();
        var response = await client.PostAsJsonAsync("api/employees", new CreateEmployeeDto(
            "Тестов Тест", NewPhone(), null, "305"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_WithInvalidEmail_Returns400()
    {
        var client = await AuthorizedClientAsync();
        var response = await client.PostAsJsonAsync("api/employees", new CreateEmployeeDto(
            "Тестов Тест", NewPhone(), "bad", "305"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Dispatcher_CanGetTechnicians()
    {
        var client = await AuthorizedClientAsync();
        var techs = await client.GetFromJsonAsync<List<UserDto>>("api/users/technicians", JsonDefaults.Options);
        Assert.NotNull(techs);
        Assert.Equal(2, techs.Count);
    }

    [Fact]
    public async Task Technician_CannotGetTechnicians()
    {
        var client = await AuthorizedClientAsync("technician1", "Tech123");
        var response = await client.GetAsync("api/users/technicians");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Technician_CannotCreateEmployee_Returns403()
    {
        var client = await AuthorizedClientAsync("technician1", "Tech123");
        var response = await client.PostAsJsonAsync("api/employees", new CreateEmployeeDto(
            "Тестов Тест", NewPhone(), null, "305"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Technician_CannotChangeAnotherTechniciansAppeal_Returns403()
    {
        var client = await AuthorizedClientAsync("technician2", "Tech123");
        var response = await client.PutAsJsonAsync("api/appeals/3/status",
            new ChangeStatusDto(AppealStatus.InProgress, null));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetStatistics_ReturnsSeededValues()
    {
        var client = await AuthorizedClientAsync();
        var stats = await client.GetFromJsonAsync<AppealStatisticsDto>("api/appeals/statistics", JsonDefaults.Options);
        Assert.NotNull(stats);
        Assert.Equal(7, stats.Total);
        Assert.Equal(3, stats.Open);
        Assert.Equal(3, stats.Completed);
        Assert.Equal(10000m, stats.Revenue);
    }

    [Fact]
    public async Task Technician_GetAll_ReturnsOnlyOwnAppeals()
    {
        var client = await AuthorizedClientAsync("technician1", "Tech123");
        var appeals = await client.GetFromJsonAsync<List<AppealDto>>("api/appeals", JsonDefaults.Options);
        Assert.NotNull(appeals);
        Assert.NotEmpty(appeals);
        Assert.All(appeals, a => Assert.Equal(3, a.TechnicianId));
    }

    [Fact]
    public async Task Technician_GetAnotherTechniciansAppeal_Returns403()
    {
        var client = await AuthorizedClientAsync("technician1", "Tech123");
        var response = await client.GetAsync("api/appeals/4");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Technician_GetStatistics_ReturnsOnlyOwnAppeals()
    {
        var client = await AuthorizedClientAsync("technician1", "Tech123");
        var appeals = await client.GetFromJsonAsync<List<AppealDto>>("api/appeals", JsonDefaults.Options);
        var stats = await client.GetFromJsonAsync<AppealStatisticsDto>("api/appeals/statistics", JsonDefaults.Options);
        Assert.NotNull(appeals);
        Assert.NotNull(stats);
        Assert.Equal(4, appeals.Count);
        Assert.All(appeals, a => Assert.Equal(3, a.TechnicianId));
        Assert.Equal(appeals.Count, stats.Total);
    }

    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExportAppeals_ReturnsCsvFile()
    {
        var client = await AuthorizedClientAsync();
        var response = await client.GetAsync("api/exchange/appeals/export?format=csv");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("csv", response.Content.Headers.ContentType?.MediaType ?? string.Empty);
    }
}
