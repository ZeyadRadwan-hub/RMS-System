using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using RMS_BACKEND.Repositories;
using Xunit;

namespace RMS_BACKEND.Tests;

public class IdentifierAllocationTests
{
    [Trait("Category", "IdentifierAllocation")]
    [Fact]
    public async Task Independent_requests_get_distinct_transaction_ids()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=RMS;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        await using var db1 = new ApplicationDbContext(options);
        await using var db2 = new ApplicationDbContext(options);
        var repo1 = new TransactionRepository(db1);
        var repo2 = new TransactionRepository(db2);
        var ids = await Task.WhenAll(repo1.GetNextIdAsync(), repo2.GetNextIdAsync());
        Assert.NotEqual(ids[0], ids[1]);
    }

    [Trait("Category", "IdentifierAllocation")]
    [Fact]
    public async Task Independent_employee_creations_get_distinct_ids()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=RMS;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        await using var db1 = new ApplicationDbContext(options);
        await using var db2 = new ApplicationDbContext(options);
        var repo1 = new EmployeeRepository(db1);
        var repo2 = new EmployeeRepository(db2);
        var ids = await Task.WhenAll(repo1.GetNextIdAsync(), repo2.GetNextIdAsync());
        Assert.NotEqual(ids[0], ids[1]);
        Assert.All(ids, id => Assert.True(id > 5));
    }
}
