using Microsoft.Data.SqlClient;
using Xunit;

namespace RMS_BACKEND.Tests;

public class DatabaseConstraintTests
{
    private const string Connection =
        @"Server=(localdb)\MSSQLLocalDB;Database=RMS;Trusted_Connection=True;TrustServerCertificate=True;";

    [Trait("Category", "DatabaseConstraints")]
    [Theory]
    [InlineData("UPDATE dbo.Transactions SET EndDate='2026-02-01' WHERE Id=1")]
    [InlineData("UPDATE dbo.TransactionTypes SET Unit=-5 WHERE Id=2")]
    [InlineData("UPDATE dbo.TransactionTypes SET Sign=99 WHERE Id=2")]
    [InlineData("UPDATE dbo.Employees SET ManagerId=Id WHERE Id=2")]
    [InlineData("UPDATE dbo.Employees SET ManagerId=4 WHERE Id=1")]
    [InlineData("UPDATE dbo.Employees SET EmployeeRole=99 WHERE Id=2")]
    [InlineData("UPDATE dbo.Employees SET Password='' WHERE Id=2")]
    [InlineData("UPDATE dbo.Employees SET DateOfEmployment='2099-01-01' WHERE Id=2")]
    [InlineData("UPDATE dbo.Employees SET DepartmentID=1 WHERE Id=2")]
    [InlineData("UPDATE dbo.Transactions SET StatusID=7 WHERE Id=1")]
    [InlineData("UPDATE dbo.Statuses SET StatusType='Department' WHERE Id=1")]
    [InlineData("UPDATE dbo.Statuses SET StatusType='TransactionStatus' WHERE Id=7")]
    [InlineData("INSERT INTO dbo.Transactions(Id,EmployeeId,TransactionTypesID,StartDate,EndDate,StatusID,CreationDate) VALUES(2000000002,2,1,'2026-03-02','2026-03-04',1,'2026-02-01')")]
    public async Task Invalid_data_is_rejected_without_changing_existing_rows(string sql)
    {
        await using var connection = new SqlConnection(Connection);
        await connection.OpenAsync();
        Assert.Equal("RMS", connection.Database);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        try
        {
            await using var command = new SqlCommand(sql, connection, transaction);
            await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    [Trait("Category", "DatabaseConstraints")]
    [Theory]
    [InlineData("UPDATE dbo.Transactions SET EndDate=StartDate WHERE Id=1")]
    [InlineData("UPDATE dbo.TransactionTypes SET Unit=0.5, Sign=-1 WHERE Id=3")]
    [InlineData("UPDATE dbo.Employees SET DepartmentID=7 WHERE Id=2")]
    [InlineData("INSERT INTO dbo.Transactions(Id,EmployeeId,TransactionTypesID,StartDate,EndDate,StatusID,CreationDate) VALUES(2000000002,2,1,'2026-03-06','2026-03-07',1,'2026-02-01')")]
    public async Task Valid_data_is_still_allowed_and_rolled_back_after_test(string sql)
    {
        await using var connection = new SqlConnection(Connection);
        await connection.OpenAsync();
        Assert.Equal("RMS", connection.Database);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        try
        {
            await using var command = new SqlCommand(sql, connection, transaction);
            Assert.Equal(1, await command.ExecuteNonQueryAsync());
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }
}
