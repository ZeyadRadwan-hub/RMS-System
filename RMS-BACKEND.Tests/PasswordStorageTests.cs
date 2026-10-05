using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using Xunit;

namespace RMS_BACKEND.Tests;

public class PasswordStorageTests
{
    [Trait("Category", "PasswordStorage")]
    [Fact]
    public async Task Existing_accounts_have_individual_nonempty_password_hashes()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=RMS;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        await using var db = new ApplicationDbContext(options);
        Assert.Equal("RMS", db.Database.GetDbConnection().Database);
        var hashes = await db.Employees.AsNoTracking().Select(e => e.Password).ToListAsync();
        Assert.NotEmpty(hashes);
        Assert.True(hashes.All(hash => hash.StartsWith("AQAAAA", StringComparison.Ordinal)
            && hash.Length >= 80), "One or more account credentials are not strong password hashes.");
        Assert.Equal(hashes.Count, hashes.Distinct(StringComparer.Ordinal).Count());
    }

    [Trait("Category", "PasswordStorage")]
    [Fact]
    public void Password_hash_verifier_accepts_only_the_matching_secret()
    {
        var employee = new RMS_BACKEND.Models.Employee { Id = 123 };
        var hasher = new PasswordHasher<RMS_BACKEND.Models.Employee>();
        var hash = hasher.HashPassword(employee, "synthetic-test-password");
        Assert.Equal(PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(employee, hash, "synthetic-test-password"));
        Assert.Equal(PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(employee, hash, "wrong-test-password"));
    }
}
