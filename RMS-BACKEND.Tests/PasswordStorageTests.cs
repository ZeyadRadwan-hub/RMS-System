using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using RMS_BACKEND.Repositories;
using Xunit;

namespace RMS_BACKEND.Tests;

public class PasswordStorageTests
{
    [Trait("Category", "PasswordStorage")]
    [Fact]
    public async Task Existing_accounts_have_one_nonempty_plaintext_test_password()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=RMS;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        await using var db = new ApplicationDbContext(options);
        Assert.Equal("RMS", db.Database.GetDbConnection().Database);
        var passwords = await db.Employees.AsNoTracking().Where(e => e.Id <= 5)
            .Select(e => e.Password).ToListAsync();
        Assert.NotEmpty(passwords);
        Assert.All(passwords, password => Assert.Equal(9, password.Length));
        Assert.Single(passwords.Distinct(StringComparer.Ordinal));
    }

    [Trait("Category", "PasswordStorage")]
    [Fact]
    public async Task Employee_repository_accepts_plaintext_and_rejects_wrong_secret()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=RMS;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        await using var db = new ApplicationDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var employee = await db.Employees.FirstAsync(e => e.Id == 2);
        var originalPassword = employee.Password;
        employee.Password = "temporary9";
        await db.SaveChangesAsync();
        var repository = new EmployeeRepository(db);
        try
        {
            Assert.NotNull(await repository.AuthenticateAsync(employee.Code, "temporary9"));
            Assert.Null(await repository.AuthenticateAsync(employee.Code, "wrong-secret"));
        }
        finally
        {
            await transaction.RollbackAsync();
            employee.Password = originalPassword;
        }
    }
}
