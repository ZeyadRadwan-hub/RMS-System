using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using RMS_BACKEND.Security;
using RMS_BACKEND.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RMS_BACKEND.Tests;

public class AuthorizationTests
{
    private sealed class RmsFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        @"Server=(localdb)\MSSQLLocalDB;Database=RMS;Trusted_Connection=True;TrustServerCertificate=True;"
                }));
        }
    }

    [Trait("Category", "Dashboard")]
    [Fact]
    public async Task Pending_stats_include_requests_awaiting_HR()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(1);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // Baseline employee 4 has exactly one Pending HR request; other tests
        // may create requests for employee 2 in parallel.
        using var response = await client.PostAsJsonAsync("/api/dashboard/stats", new { employeeId = 4 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, json.RootElement.GetProperty("pendingRequests").GetInt32());
    }

    [Trait("Category", "EmployeeHierarchy")]
    [Fact]
    public async Task Manager_cycle_is_a_validation_error_not_a_server_error()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var current = await db.Employees.AsNoTracking().SingleAsync(e => e.Id == 1);
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(1);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.PutAsJsonAsync("/api/employees/1", new
        {
            current.Code, current.Name, current.DateOfEmployment,
            employeeRole = (short)current.EmployeeRole,
            current.EmployeeLevelId, current.DepartmentID, managerId = 4
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null((await db.Employees.AsNoTracking().SingleAsync(e => e.Id == 1)).ManagerId);
    }

    [Trait("Category", "Authorization")]
    [Theory]
    [InlineData("/api/employees")]
    [InlineData("/api/transactions/all")]
    [InlineData("/api/leavebalance/all")]
    public async Task Anonymous_or_forged_headers_are_not_accepted(string path)
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Employee-Id", "1");
        client.DefaultRequestHeaders.Add("X-Employee-Role", "HR");

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Trait("Category", "Authorization")]
    [Fact]
    public async Task Employee_session_cannot_become_HR_by_forging_headers()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<SessionService>();
        var token = await sessions.IssueAsync(2);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Employee-Id", "1");
        client.DefaultRequestHeaders.Add("X-Employee-Role", "HR");

        using var response = await client.GetAsync("/api/employees");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
    }

    [Trait("Category", "Authorization")]
    [Theory]
    [InlineData("GET", "/api/employees/1")]
    [InlineData("GET", "/api/employees/substitutes")]
    [InlineData("POST", "/api/employees")]
    [InlineData("PUT", "/api/employees/1")]
    [InlineData("DELETE", "/api/employees/1")]
    [InlineData("POST", "/api/employees/recalculate-managers")]
    [InlineData("GET", "/api/transactions/1")]
    [InlineData("GET", "/api/transactions/my-requests")]
    [InlineData("GET", "/api/transactions/my-team-requests")]
    [InlineData("POST", "/api/transactions")]
    [InlineData("PUT", "/api/transactions/1")]
    [InlineData("POST", "/api/transactions/1/cancel")]
    [InlineData("POST", "/api/transactions/1/approve")]
    [InlineData("POST", "/api/transactions/1/reject")]
    [InlineData("POST", "/api/transactions/filter")]
    [InlineData("GET", "/api/leavebalance/2")]
    [InlineData("GET", "/api/leavebalance/my-balance")]
    [InlineData("GET", "/api/leavebalance/team-balances")]
    [InlineData("GET", "/api/leavebalance/department/7")]
    [InlineData("POST", "/api/dashboard/stats")]
    [InlineData("POST", "/api/dashboard/charts")]
    [InlineData("GET", "/api/auth/me")]
    [InlineData("POST", "/api/auth/logout")]
    [InlineData("POST", "/api/auth/change-password")]
    public async Task Every_nonlogin_route_rejects_anonymous_requests(string method, string path)
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Add("X-Employee-Id", "1");
        request.Headers.Add("X-Employee-Role", "HR");
        if (method is "POST" or "PUT") request.Content = JsonContent.Create(new { });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Trait("Category", "Authorization")]
    [Fact]
    public async Task Employee_session_is_scoped_and_logout_revokes_it()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(2);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Employee-Id", "1");
        client.DefaultRequestHeaders.Add("X-Employee-Role", "HR");

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/transactions/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/transactions/3")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/transactions/all")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/leavebalance/3")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Trait("Category", "Authorization")]
    [Fact]
    public async Task Cross_team_manager_cannot_approve_or_reject()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(4);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var approve = await client.PostAsJsonAsync("/api/transactions/1/approve", new { responseMessage = "test" });
        using var reject = await client.PostAsJsonAsync("/api/transactions/1/reject", new { responseMessage = "test" });
        Assert.Equal(HttpStatusCode.Forbidden, approve.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reject.StatusCode);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.Transactions.AsNoTracking().Where(t => t.Id == 1).Select(t => t.StatusID).SingleAsync());
        await client.PostAsync("/api/auth/logout", null);
    }

    [Trait("Category", "Authorization")]
    [Fact]
    public async Task Expired_session_is_rejected()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(2);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var bytes = Convert.FromBase64String(token.Replace('-', '+').Replace('_', '/') + "=");
        var hash = SHA256.HashData(bytes);
        var session = await db.AuthSessions.SingleAsync(s => s.TokenHash == hash);
        session.CreatedUtc = DateTime.UtcNow.AddHours(-2);
        session.ExpiresUtc = DateTime.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Trait("Category", "Authorization")]
    [Fact]
    public async Task Employee_filters_and_dashboard_never_escape_own_scope()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(2);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Employee-Role", "HR");

        using var filtered = await client.PostAsJsonAsync("/api/transactions/filter", new { });
        Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
        using var filteredJson = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        Assert.All(filteredJson.RootElement.EnumerateArray(), item =>
            Assert.Equal(2, item.GetProperty("employeeId").GetInt32()));

        using var stats = await client.PostAsJsonAsync("/api/dashboard/stats", new { });
        Assert.Equal(HttpStatusCode.OK, stats.StatusCode);
        using var statsJson = JsonDocument.Parse(await stats.Content.ReadAsStringAsync());
        Assert.Equal(1, statsJson.RootElement.GetProperty("totalRequests").GetInt32());

        using var substitutes = await client.GetAsync("/api/employees/substitutes");
        Assert.Equal(HttpStatusCode.OK, substitutes.StatusCode);
        using var substituteJson = JsonDocument.Parse(await substitutes.Content.ReadAsStringAsync());
        Assert.All(substituteJson.RootElement.EnumerateArray(), item =>
            Assert.Equal(7, item.GetProperty("departmentID").GetInt32()));
        await client.PostAsync("/api/auth/logout", null);
    }

    [Trait("Category", "Authorization")]
    [Fact]
    public async Task HR_session_can_access_organization_data_even_with_employee_headers()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(1);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Employee-Id", "2");
        client.DefaultRequestHeaders.Add("X-Employee-Role", "Employee");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/employees")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/transactions/all")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/leavebalance/all")).StatusCode);
        await client.PostAsync("/api/auth/logout", null);
    }

    [Trait("Category", "Authorization")]
    [Fact]
    public async Task Password_change_rejects_wrong_current_secret_without_changing_account()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var before = await db.Employees.AsNoTracking().Where(e => e.Id == 2).Select(e => e.Password).SingleAsync();
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(2);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = "intentionally-wrong-password", newPassword = "another-strong-test-password" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await db.Employees.AsNoTracking().Where(e => e.Id == 2).Select(e => e.Password).SingleAsync());
        await client.PostAsync("/api/auth/logout", null);
    }

    [Trait("Category", "Authorization")]
    [Fact]
    public async Task Login_rate_limit_rejects_repeated_attempts()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var guard = scope.ServiceProvider.GetRequiredService<LoginAttemptGuard>();
        var code = "RMS_LIMIT_TEST_" + Guid.NewGuid().ToString("N");
        try
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                using var response = await client.PostAsJsonAsync("/api/auth/login",
                    new { code, password = "wrong" });
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
            using var limited = await client.PostAsJsonAsync("/api/auth/login",
                new { code, password = "wrong" });
            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        }
        finally { await guard.ClearAsync(code); }
    }

    [Trait("Category", "EmployeeLifecycle")]
    [Fact]
    public async Task Invalid_employee_role_is_rejected_before_any_insert()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var code = "RMS_AUTOTEST_ROLE_" + Guid.NewGuid().ToString("N");
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(1);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            using var response = await client.PostAsJsonAsync("/api/employees", new
            {
                code, name = "Synthetic invalid role", password = "synthetic-test-password",
                dateOfEmployment = "2020-01-01", employeeRole = 9,
                employeeLevelId = 1, departmentID = 7
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(await db.Employees.AsNoTracking().AnyAsync(e => e.Code == code));
        }
        finally { await client.PostAsync("/api/auth/logout", null); }
    }

    [Trait("Category", "Authorization")]
    [Fact]
    public async Task Untrusted_browser_origin_is_not_allowed_by_CORS()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/employees");
        request.Headers.Add("Origin", "https://untrusted.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        using var response = await client.SendAsync(request);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
