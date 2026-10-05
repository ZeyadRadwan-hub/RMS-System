using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using RMS_BACKEND.Models;

if (args.Length != 1)
{
    Console.Error.WriteLine("Expected one absolute path for the protected provisioning file.");
    return 2;
}

var output = Path.GetFullPath(args[0]);
var allowedDirectory = Path.GetFullPath(@"C:\Users\workstation\AppData\Local\RMS-Repair-Backups\Provisioning");
if (!string.Equals(Path.GetDirectoryName(output), allowedDirectory, StringComparison.OrdinalIgnoreCase)
    || File.Exists(output))
{
    Console.Error.WriteLine("Output must be a new file in the protected RMS provisioning directory.");
    return 2;
}

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=RMS;Trusted_Connection=True;TrustServerCertificate=True;")
    .Options;
await using var db = new ApplicationDbContext(options);
await db.Database.OpenConnectionAsync();
if (!string.Equals(db.Database.GetDbConnection().Database, "RMS", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Wrong database; no changes made.");
    return 2;
}

var employees = await db.Employees.OrderBy(e => e.Id).ToListAsync();
if (employees.Count == 0 || employees.Any(e => e.Password.StartsWith("AQAAAA", StringComparison.Ordinal)))
{
    Console.Error.WriteLine("Expected only legacy un-hashed accounts; no changes made.");
    return 2;
}

var hasher = new PasswordHasher<Employee>();
var credentials = new List<(int Id, string Code, string Secret)>();
foreach (var employee in employees)
{
    var secretBytes = RandomNumberGenerator.GetBytes(32);
    var secret = Convert.ToBase64String(secretBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    credentials.Add((employee.Id, employee.Code, secret));
    employee.Password = hasher.HashPassword(employee, secret);
}

var lines = new List<string>
{
    "RMS test database — one-time generated credentials. Keep this file private; rotate in the application.",
    "EmployeeId\tEmployeeCode\tPassword"
};
lines.AddRange(credentials.Select(c => $"{c.Id}\t{c.Code}\t{c.Secret}"));

var committed = false;
try
{
    await using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None,
        bufferSize: 4096, FileOptions.WriteThrough))
    await using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
    {
        foreach (var line in lines) await writer.WriteLineAsync(line);
    }

    await using var transaction = await db.Database.BeginTransactionAsync();
    foreach (var employee in employees)
        db.Entry(employee).Property(e => e.Password).IsModified = true;
    await db.SaveChangesAsync();
    await transaction.CommitAsync();
    committed = true;
    Console.WriteLine($"Rotated and hashed {employees.Count} RMS test accounts. Provisioning file: {output}");
    return 0;
}
catch
{
    if (!committed && File.Exists(output)) File.Delete(output);
    throw;
}
