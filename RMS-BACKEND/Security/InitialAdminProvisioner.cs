using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using RMS_BACKEND.Models;

namespace RMS_BACKEND.Security;

// Interactive, one-time bootstrap. No default credential and no secret in command-line arguments.
public static class InitialAdminProvisioner
{
    public static async Task EnsureEmptyRmsAsync(ApplicationDbContext db)
    {
        if (db.Database.GetDbConnection().Database != "RMS")
            throw new InvalidOperationException("Provisioning is restricted to RMS");
        if (await db.Employees.AnyAsync())
            throw new InvalidOperationException("Existing employees found; bootstrap is disabled to preserve account history");
        if (!await db.Statuses.AnyAsync(s => s.Id == 10 && s.StatusType == "Department") ||
            !await db.EmployeeLevels.AnyAsync(l => l.Id == 2))
            throw new InvalidOperationException("RMS lookups are missing; run database-setup.sql first");
    }

    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await EnsureEmptyRmsAsync(db);
        if (Console.IsInputRedirected)
            throw new InvalidOperationException("An interactive console is required for initial HR provisioning");

        Console.Write("Initial HR employee code: ");
        var code = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Initial HR employee name: ");
        var name = Console.ReadLine()?.Trim() ?? "";
        if (code.Length is < 1 or > 50 || name.Length is < 1 or > 200)
            throw new InvalidOperationException("Code and name are required and must fit the RMS schema");
        Console.Write("Password (12–128 characters; hidden): ");
        var password = ReadSecret();
        Console.Write("Confirm password (hidden): ");
        var confirmation = ReadSecret();
        if (password.Length is < 12 or > 128 || password != confirmation)
            throw new InvalidOperationException("Passwords differ or violate the 12–128 character requirement");

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await EnsureEmptyRmsAsync(db);
        var employee = new Employee
        {
            Id = 1, Code = code, Name = name, DepartmentID = 10, EmployeeLevelId = 2,
            EmployeeRole = EmployeeRole.Manager, DateOfEmployment = DateTime.Today, IsDeleted = false
        };
        employee.Password = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Employee>>()
            .HashPassword(employee, password);
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        Console.WriteLine("Initial HR account provisioned; no password was written to a file or log.");
    }

    private static string ReadSecret()
    {
        var chars = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); break; }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (chars.Count > 0) chars.RemoveAt(chars.Count - 1);
                continue;
            }
            if (!char.IsControl(key.KeyChar) && chars.Count < 128) chars.Add(key.KeyChar);
        }
        return new string(chars.ToArray());
    }
}
