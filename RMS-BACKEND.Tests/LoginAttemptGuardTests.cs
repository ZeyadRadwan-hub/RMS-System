using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RMS_BACKEND.Security;
using RMS_BACKEND.Data;
using Xunit;

namespace RMS_BACKEND.Tests;

public class LoginAttemptGuardTests
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

    [Trait("Category", "AccountRateLimit")]
    [Fact]
    public async Task One_account_is_limited_after_five_attempts_independent_of_ip()
    {
        using var factory = new RmsFactory();
        using var scope = factory.Services.CreateScope();
        var guard = scope.ServiceProvider.GetRequiredService<LoginAttemptGuard>();
        var code = "RMS_AUTOTEST_LIMIT_" + Guid.NewGuid().ToString("N");
        try
        {
            for (var i = 0; i < 5; i++) Assert.True(await guard.TryAcquireAsync(code));
            Assert.False(await guard.TryAcquireAsync(code));
        }
        finally { await guard.ClearAsync(code); }
    }

    [Trait("Category", "Bootstrap")]
    [Fact]
    public async Task Bootstrap_refuses_to_touch_populated_RMS()
    {
        using var factory = new RmsFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => InitialAdminProvisioner.EnsureEmptyRmsAsync(db));
    }
}
