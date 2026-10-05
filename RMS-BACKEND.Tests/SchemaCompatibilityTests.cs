using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using Xunit;

namespace RMS_BACKEND.Tests;

public class SchemaCompatibilityTests
{
    private const string AuthorizedConnection =
        @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=RMS;Integrated Security=True;Encrypt=False";

    [Fact]
    [Trait("Category", "SchemaCompatibility")]
    public async Task All_entity_sets_are_queryable_from_authorized_RMS_schema()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(AuthorizedConnection)
            .Options;
        await using var db = new ApplicationDbContext(options);

        Assert.Equal("RMS", db.Database.GetDbConnection().Database);
        Assert.True(await db.EmployeeLevels.CountAsync() >= 2);
        Assert.True(await db.Statuses.CountAsync() >= 11);
        Assert.True(await db.TransactionTypes.CountAsync() >= 5);
        Assert.True(await db.Employees.CountAsync() >= 5);
        Assert.True(await db.Transactions.CountAsync() >= 3);

        // CountAsync alone does not select mapped columns or related entities.
        Assert.NotNull(await db.Employees.Include(e => e.EmployeeLevel)
            .Include(e => e.Department).FirstAsync());
        Assert.NotNull(await db.Transactions.Include(t => t.TransactionType)
            .Include(t => t.Status).FirstAsync());
    }
}
