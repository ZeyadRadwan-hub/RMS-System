using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RMS_BACKEND.Data;
using RMS_BACKEND.Controllers;
using RMS_BACKEND.DTOs;
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

    [Trait("Category", "EmployeeHierarchy")]
    [Fact]
    public async Task Reassigning_the_last_direct_report_demotes_the_old_manager()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=RMS;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        await using var db = new ApplicationDbContext(options);
        Assert.Equal("RMS", db.Database.GetDbConnection().Database);
        try
        {
            var hasher = new PasswordHasher<Employee>();
            var oldManager = new Employee
            {
                Id = 2000000010,
                Code = "RMS_TEST_OLD_MANAGER",
                Name = "Temporary old manager",
                DepartmentID = 7,
                EmployeeLevelId = 1,
                EmployeeRole = EmployeeRole.Manager,
                DateOfEmployment = new DateTime(2020, 1, 1)
            };
            oldManager.Password = hasher.HashPassword(oldManager, "synthetic-test-password");
            var report = new Employee
            {
                Id = 2000000011,
                Code = "RMS_TEST_REASSIGN",
                Name = "Temporary report",
                DepartmentID = 7,
                EmployeeLevelId = 1,
                EmployeeRole = EmployeeRole.Employee,
                ManagerId = oldManager.Id,
                DateOfEmployment = new DateTime(2020, 1, 1)
            };
            report.Password = hasher.HashPassword(report, "synthetic-test-password");
            db.Employees.AddRange(oldManager, report);
            await db.SaveChangesAsync();

            var controller = new EmployeesController(new EmployeeRepository(db, hasher), hasher, db)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider()
                    }
                }
            };
            var action = await controller.UpdateEmployee(report.Id, new UpdateEmployeeDto
            {
                Code = report.Code,
                Name = report.Name,
                DateOfEmployment = report.DateOfEmployment,
                EmployeeRole = (short)EmployeeRole.Employee,
                EmployeeLevelId = report.EmployeeLevelId,
                ManagerId = null,
                DepartmentID = report.DepartmentID
            });

            Assert.IsType<OkObjectResult>(action.Result);
            Assert.Equal(EmployeeRole.Employee,
                await db.Employees.AsNoTracking().Where(e => e.Id == oldManager.Id)
                    .Select(e => e.EmployeeRole).SingleAsync());
        }
        finally
        {
            var testEmployees = await db.Employees
                .Where(e => e.Id == 2000000010 || e.Id == 2000000011).ToListAsync();
            if (testEmployees.Count > 0)
            {
                foreach (var employee in testEmployees) employee.ManagerId = null;
                await db.SaveChangesAsync();
                db.Employees.RemoveRange(testEmployees);
                await db.SaveChangesAsync();
            }
        }
    }
}
