using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using RMS_BACKEND.Models;
using RMS_BACKEND.Services;
using Xunit;

namespace RMS_BACKEND.Tests;

public class LeaveBalanceBehaviorTests
{
    private static ApplicationDbContext NewContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=RMS;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options);

    [Trait("Category", "LeaveBalance")]
    [Theory]
    [InlineData(4, "2026-09-15", "2026-09-15", "2026-10-04", 1.0)]
    [InlineData(2, "2025-12-30", "2026-01-03", "2026-01-02", -2.0)]
    [InlineData(2, "2026-09-29", "2026-10-05", "2026-10-02", -4.0)]
    public async Task Approved_request_applies_only_signed_days_in_selected_year_and_asof(
        int typeId, string start, string end, string asOf, double expectedChange)
    {
        await using var db = NewContext();
        Assert.Equal("RMS", db.Database.GetDbConnection().Database);
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var balance = new LeaveBalanceService(db, new LeaveCalculationService());
            var before = await balance.GetLeaveBalanceAsync(2, DateTime.Parse(asOf));
            db.Transactions.Add(new Transaction
            {
                Id = 2000000000,
                EmployeeId = 2,
                TransactionTypesID = typeId,
                StartDate = DateTime.Parse(start),
                EndDate = DateTime.Parse(end),
                StatusID = TransactionStatus.ApprovedByHR,
                CreationDate = DateTime.Parse("2025-12-01")
            });
            await db.SaveChangesAsync();
            var after = await balance.GetLeaveBalanceAsync(2, DateTime.Parse(asOf));
            Assert.Equal(expectedChange, after.LeaveBalance - before.LeaveBalance, precision: 5);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    [Trait("Category", "LeaveBalance")]
    [Fact]
    public async Task New_hire_in_probation_has_no_accrual()
    {
        await using var db = NewContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var employee = new Employee
            {
                Id = 2000000000,
                Code = "RMS_TEST_PROBATION",
                Name = "Temporary test employee",
                DepartmentID = 7,
                EmployeeLevelId = 1,
                EmployeeRole = EmployeeRole.Employee,
                DateOfEmployment = new DateTime(2026, 7, 1)
            };
            employee.Password = new PasswordHasher<Employee>().HashPassword(employee, "synthetic-test-password");
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            var balance = await new LeaveBalanceService(db, new LeaveCalculationService())
                .GetLeaveBalanceAsync(employee.Id, new DateTime(2026, 10, 4));
            Assert.True(balance.IsInProbation);
            Assert.Equal(0, balance.TotalAccruedLeave);
            Assert.Equal(0, balance.LeaveBalance);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }
}
