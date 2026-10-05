using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace RMS_BACKEND.Data;

public static class SequenceIdAllocator
{
    public static Task<int> NextTransactionIdAsync(ApplicationDbContext db) =>
        NextAsync(db, "dbo.RMS_TransactionIdSequence");

    public static Task<int> NextEmployeeIdAsync(ApplicationDbContext db) =>
        NextAsync(db, "dbo.RMS_EmployeeIdSequence");

    private static async Task<int> NextAsync(ApplicationDbContext db, string sequenceName)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await db.Database.OpenConnectionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT NEXT VALUE FOR {sequenceName}";
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        finally
        {
            if (openedHere) await db.Database.CloseConnectionAsync();
        }
    }
}
