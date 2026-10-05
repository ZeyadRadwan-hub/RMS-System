using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RMS_BACKEND.Data;
using RMS_BACKEND.Security;
using Xunit;

namespace RMS_BACKEND.Tests;

public class TransactionValidationTests
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

    [Trait("Category", "Performance")]
    [Fact]
    public async Task Organization_request_lists_are_pageable_and_bounded()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(1));

        using var response = await client.GetAsync("/api/transactions/all?page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.True(root.TryGetProperty("items", out var items));
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(1, root.GetProperty("pageSize").GetInt32());
        var totalCount = root.GetProperty("totalCount").GetInt32();
        Assert.True(totalCount >= 2);
        Assert.True(root.GetProperty("hasNext").GetBoolean());
    }

    [Trait("Category", "Validation")]
    [Theory]
    [InlineData("/api/transactions", "{\"transactionTypesID\":0,\"startDate\":\"2026-10-10\",\"endDate\":\"2026-10-10\",\"leaveRationale\":\"DTO_TEST_INVALID\"}")]
    [InlineData("/api/transactions", "{\"transactionTypesID\":2,\"startDate\":\"2026-10-11\",\"endDate\":\"2026-10-10\",\"leaveRationale\":\"DTO_TEST_INVALID\"}")]
    [InlineData("/api/employees", "{\"code\":\"DTO_TEST_INVALID\",\"name\":\"test\",\"password\":\"short\",\"employeeRole\":9,\"employeeLevelId\":0,\"departmentID\":0}")]
    [InlineData("/api/dashboard/stats", "{\"employeeId\":-1}")]
    [InlineData("/api/transactions/filter", "{\"groupBy\":\"arbitrary\"}")]
    public async Task Invalid_json_contracts_return_400_without_writes(string endpoint, string json)
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var employeeCount = await db.Employees.CountAsync(e => e.Code == "DTO_TEST_INVALID");
        var transactionCount = await db.Transactions.CountAsync(t => t.LeaveRationale == "DTO_TEST_INVALID");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(1));
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(endpoint, content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(result.RootElement.TryGetProperty("errors", out _));
        Assert.Equal(employeeCount, await db.Employees.CountAsync(e => e.Code == "DTO_TEST_INVALID"));
        Assert.Equal(transactionCount, await db.Transactions.CountAsync(t => t.LeaveRationale == "DTO_TEST_INVALID"));
    }

    [Trait("Category", "Concurrency")]
    [Fact]
    public async Task Fifty_independent_concurrent_requests_receive_unique_ids()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(2);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var marker = "RMS_AUTOTEST_50_" + Guid.NewGuid().ToString("N");
        try
        {
            var start = new DateTime(2027, 1, 1);
            var operations = Enumerable.Range(0, 50).Select(offset =>
            {
                var date = start.AddDays(offset).ToString("yyyy-MM-dd");
                return client.PostAsJsonAsync("/api/transactions", new
                {
                    transactionTypesID = 4, startDate = date, endDate = date, leaveRationale = marker
                });
            });
            var responses = await Task.WhenAll(operations);
            try
            {
                Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
                var ids = await db.Transactions.AsNoTracking().Where(t => t.LeaveRationale == marker)
                    .Select(t => t.Id).ToListAsync();
                Assert.Equal(50, ids.Count);
                Assert.Equal(50, ids.Distinct().Count());
            }
            finally { foreach (var response in responses) response.Dispose(); }
        }
        finally
        {
            db.Transactions.RemoveRange(await db.Transactions.Where(t => t.LeaveRationale == marker).ToListAsync());
            await db.SaveChangesAsync();
        }
    }

    [Trait("Category", "Concurrency")]
    [Fact]
    public async Task Two_simultaneous_overlapping_requests_cannot_both_be_saved()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(2));
        var marker = "RMS_AUTOTEST_OVERLAP_RACE_" + Guid.NewGuid().ToString("N");
        try
        {
            var payload = new
            {
                transactionTypesID = 4, startDate = "2027-07-01", endDate = "2027-07-01",
                leaveRationale = marker
            };
            var responses = await Task.WhenAll(
                client.PostAsJsonAsync("/api/transactions", payload),
                client.PostAsJsonAsync("/api/transactions", payload));
            try
            {
                Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
                Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest));
                Assert.Equal(1, await db.Transactions.AsNoTracking()
                    .CountAsync(t => t.LeaveRationale == marker));
            }
            finally { foreach (var response in responses) response.Dispose(); }
        }
        finally
        {
            db.Transactions.RemoveRange(await db.Transactions.Where(t => t.LeaveRationale == marker).ToListAsync());
            await db.SaveChangesAsync();
        }
    }

    [Trait("Category", "Concurrency")]
    [Fact]
    public async Task Competing_final_decisions_record_exactly_one_winner()
    {
        using var factory = new RmsFactory();
        using var employeeClient = factory.CreateClient();
        using var hrClient = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sessionService = scope.ServiceProvider.GetRequiredService<SessionService>();
        employeeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await sessionService.IssueAsync(2));
        hrClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await sessionService.IssueAsync(1));
        var marker = "RMS_AUTOTEST_DECISION_RACE_" + Guid.NewGuid().ToString("N");
        int? requestId = null;
        try
        {
            using var created = await employeeClient.PostAsJsonAsync("/api/transactions", new
            {
                transactionTypesID = 2, startDate = "2026-12-27", endDate = "2026-12-27",
                leaveRationale = marker
            });
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            requestId = await db.Transactions.AsNoTracking().Where(t => t.LeaveRationale == marker)
                .Select(t => t.Id).SingleAsync();
            var actions = await Task.WhenAll(
                hrClient.PostAsJsonAsync($"/api/transactions/{requestId}/approve", new { responseMessage = "approve" }),
                hrClient.PostAsJsonAsync($"/api/transactions/{requestId}/reject", new { responseMessage = "reject" }));
            try
            {
                Assert.Equal(1, actions.Count(r => r.StatusCode == HttpStatusCode.OK));
                Assert.Equal(1, await db.RequestDecisionAudit.AsNoTracking()
                    .CountAsync(a => a.TransactionId == requestId));
            }
            finally { foreach (var response in actions) response.Dispose(); }
        }
        finally
        {
            if (requestId.HasValue)
            {
                db.RequestDecisionAudit.RemoveRange(await db.RequestDecisionAudit
                    .Where(a => a.TransactionId == requestId).ToListAsync());
                await db.SaveChangesAsync();
                db.Transactions.RemoveRange(await db.Transactions.Where(t => t.Id == requestId).ToListAsync());
                await db.SaveChangesAsync();
            }
        }
    }

    [Trait("Category", "DecisionAudit")]
    [Fact]
    public async Task Hr_approval_records_actor_and_transition_in_database()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var employeeToken = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(2);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var marker = "RMS_AUTOTEST_AUDIT_" + Guid.NewGuid().ToString("N");
        int? transactionId = null;
        try
        {
            using var created = await client.PostAsJsonAsync("/api/transactions", new
            {
                transactionTypesID = 2, startDate = "2026-11-18", endDate = "2026-11-18",
                leaveRationale = marker
            });
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            transactionId = await db.Transactions.AsNoTracking()
                .Where(t => t.LeaveRationale == marker).Select(t => t.Id).SingleAsync();
            var hrToken = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(1);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hrToken);
            using var approved = await client.PostAsJsonAsync(
                $"/api/transactions/{transactionId}/approve", new { responseMessage = "Audit test" });
            Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
            var actor = await db.Database.SqlQueryRaw<int>(
                "SELECT ActorEmployeeId AS Value FROM dbo.RequestDecisionAudit WHERE TransactionId = {0}",
                transactionId).SingleAsync();
            var oldStatus = await db.Database.SqlQueryRaw<int>(
                "SELECT FromStatusId AS Value FROM dbo.RequestDecisionAudit WHERE TransactionId = {0}",
                transactionId).SingleAsync();
            var newStatus = await db.Database.SqlQueryRaw<int>(
                "SELECT ToStatusId AS Value FROM dbo.RequestDecisionAudit WHERE TransactionId = {0}",
                transactionId).SingleAsync();
            Assert.Equal(1, actor);
            Assert.Equal(1, oldStatus);
            Assert.Equal(3, newStatus);
        }
        finally
        {
            if (transactionId.HasValue)
            {
                if (await db.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS Value FROM sys.tables WHERE name = 'RequestDecisionAudit'").SingleAsync() > 0)
                    await db.Database.ExecuteSqlRawAsync(
                        "DELETE FROM dbo.RequestDecisionAudit WHERE TransactionId = {0}", transactionId);
                db.Transactions.RemoveRange(await db.Transactions.Where(t => t.Id == transactionId).ToListAsync());
                await db.SaveChangesAsync();
            }
        }
    }

    [Trait("Category", "TransactionValidation")]
    [Theory]
    [InlineData(2, "2026-10-10", "2026-10-09")]
    [InlineData(999, "2026-10-10", "2026-10-10")]
    [InlineData(1, "2026-03-02", "2026-03-04")]
    [InlineData(2, "2026-10-15", "2027-03-31")]
    public async Task Invalid_or_overlapping_or_unaffordable_request_is_rejected_without_SQL_leak(
        int typeId, string start, string end)
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal("RMS", db.Database.GetDbConnection().Database);
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(2);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var marker = "RMS_AUTOTEST_VALIDATION_" + Guid.NewGuid().ToString("N");
        try
        {
            using var response = await client.PostAsJsonAsync("/api/transactions", new
            {
                transactionTypesID = typeId,
                startDate = start,
                endDate = end,
                leaveRationale = marker
            });
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.DoesNotContain("CK_RMS_", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("dbo.", body, StringComparison.OrdinalIgnoreCase);
            Assert.False(await db.Transactions.AsNoTracking().AnyAsync(t => t.LeaveRationale == marker));
        }
        finally
        {
            // Only rows created by this exact test marker are eligible for cleanup.
            var testRows = await db.Transactions.Where(t => t.LeaveRationale == marker).ToListAsync();
            db.Transactions.RemoveRange(testRows);
            await db.SaveChangesAsync();
            await client.PostAsync("/api/auth/logout", null);
        }
    }

    [Trait("Category", "TransactionValidation")]
    [Fact]
    public async Task Valid_one_day_request_can_be_created_and_read_back()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(2);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var marker = "RMS_AUTOTEST_VALID_" + Guid.NewGuid().ToString("N");
        try
        {
            using var created = await client.PostAsJsonAsync("/api/transactions", new
            {
                transactionTypesID = 2,
                startDate = "2026-11-10",
                endDate = "2026-11-10",
                leaveRationale = marker
            });
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            var row = await db.Transactions.AsNoTracking().SingleAsync(t => t.LeaveRationale == marker);
            Assert.Equal(2, row.EmployeeId);
            Assert.Equal(1, row.StatusID);
            using var read = await client.GetAsync($"/api/transactions/{row.Id}");
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        }
        finally
        {
            var testRows = await db.Transactions.Where(t => t.LeaveRationale == marker).ToListAsync();
            db.Transactions.RemoveRange(testRows);
            await db.SaveChangesAsync();
            await client.PostAsync("/api/auth/logout", null);
        }
    }

    [Trait("Category", "MedicalDocuments")]
    [Fact]
    public async Task Sick_leave_without_a_medical_document_is_rejected()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(2);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var marker = "RMS_AUTOTEST_SICK_NODOC_" + Guid.NewGuid().ToString("N");
        try
        {
            using var response = await client.PostAsJsonAsync("/api/transactions", new
            {
                transactionTypesID = 1,
                startDate = "2026-12-15",
                endDate = "2026-12-15",
                leaveRationale = marker
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(await db.Transactions.AsNoTracking().AnyAsync(t => t.LeaveRationale == marker));
        }
        finally
        {
            db.Transactions.RemoveRange(await db.Transactions.Where(t => t.LeaveRationale == marker).ToListAsync());
            await db.SaveChangesAsync();
            await client.PostAsync("/api/auth/logout", null);
        }
    }

    [Trait("Category", "MedicalDocuments")]
    [Fact]
    public async Task Sick_leave_accepts_a_real_multipart_document()
    {
        using var factory = new RmsFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var token = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(2);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var marker = "RMS_AUTOTEST_SICK_PDF_" + Guid.NewGuid().ToString("N");
        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("1"), "TransactionTypesID");
            form.Add(new StringContent("2026-12-16"), "StartDate");
            form.Add(new StringContent("2026-12-16"), "EndDate");
            form.Add(new StringContent(marker), "LeaveRationale");
            var file = new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4\nsynthetic-test-content"));
            file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(file, "MedicalDocuments", "test-note.pdf");
            using var response = await client.PostAsync("/api/transactions", form);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var row = await db.Transactions.AsNoTracking().SingleAsync(t => t.LeaveRationale == marker);
            var document = await db.MedicalDocuments.AsNoTracking().SingleAsync(d => d.TransactionId == row.Id);
            Assert.Equal("application/pdf", document.MimeType);
            using var listed = await client.GetAsync($"/api/transactions/{row.Id}/medical-documents");
            Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
            using var downloaded = await client.GetAsync($"/api/transactions/{row.Id}/medical-documents/{document.Id}");
            Assert.Equal(HttpStatusCode.OK, downloaded.StatusCode);
            Assert.Equal(document.Content, await downloaded.Content.ReadAsByteArrayAsync());
            client.DefaultRequestHeaders.Authorization = null;
            using var anonymous = await client.GetAsync($"/api/transactions/{row.Id}/medical-documents/{document.Id}");
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            var otherToken = await scope.ServiceProvider.GetRequiredService<SessionService>().IssueAsync(3);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
            using var otherEmployee = await client.GetAsync($"/api/transactions/{row.Id}/medical-documents/{document.Id}");
            Assert.Equal(HttpStatusCode.Forbidden, otherEmployee.StatusCode);
            await client.PostAsync("/api/auth/logout", null);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        finally
        {
            var ids = await db.Transactions.Where(t => t.LeaveRationale == marker).Select(t => t.Id).ToListAsync();
            await db.MedicalDocuments.Where(d => ids.Contains(d.TransactionId)).ExecuteDeleteAsync();
            await db.Transactions.Where(t => ids.Contains(t.Id)).ExecuteDeleteAsync();
            await client.PostAsync("/api/auth/logout", null);
        }
    }
}
