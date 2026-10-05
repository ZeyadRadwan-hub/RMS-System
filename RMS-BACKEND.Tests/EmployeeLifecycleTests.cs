using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using RMS_BACKEND.Models;
using RMS_BACKEND.Repositories;
using Xunit;

namespace RMS_BACKEND.Tests;

public class EmployeeLifecycleTests
{
    [Trait("Category", "EmployeeLifecycle")]
    [Fact]
    public async Task Delete_marks_employee_inactive_without_removing_history_row()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=RMS;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        await using var db = new ApplicationDbContext(options);
        Assert.Equal("RMS", db.Database.GetDbConnection().Database);
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var employee = new Employee
            {
                Id = 2000000001,
                Code = "RMS_TEST_DELETE",
                Name = "Temporary test employee",
                DepartmentID = 7,
                EmployeeLevelId = 1,
                EmployeeRole = EmployeeRole.Employee,
                DateOfEmployment = new DateTime(2020, 1, 1)
            };
            employee.Password = new PasswordHasher<Employee>().HashPassword(employee, "synthetic-test-password");
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            var repo = new EmployeeRepository(db, new PasswordHasher<Employee>());
            Assert.True(await repo.DeleteAsync(employee.Id));
            var persisted = await db.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.Id == employee.Id);
            Assert.NotNull(persisted);
            Assert.True(persisted.IsDeleted);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }
}
