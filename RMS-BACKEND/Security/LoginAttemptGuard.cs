using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;

namespace RMS_BACKEND.Security;

// A shared SQL counter prevents distributed source IPs from bypassing account throttling.
public sealed class LoginAttemptGuard(ApplicationDbContext db)
{
    public async Task<bool> TryAcquireAsync(string code)
    {
        var hash = Hash(code);
        await using var connection = OpenRmsConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            DECLARE @now datetime2(7) = SYSUTCDATETIME();
            UPDATE dbo.LoginAttempts WITH (UPDLOCK, HOLDLOCK)
                SET AttemptCount = CASE WHEN WindowStartUtc <= DATEADD(minute, -1, @now) THEN 1
                                        WHEN AttemptCount < 6 THEN AttemptCount + 1 ELSE 6 END,
                    WindowStartUtc = CASE WHEN WindowStartUtc <= DATEADD(minute, -1, @now) THEN @now
                                          ELSE WindowStartUtc END
                WHERE AccountHash = @hash;
            IF @@ROWCOUNT = 0
                INSERT INTO dbo.LoginAttempts(AccountHash, WindowStartUtc, AttemptCount)
                    VALUES(@hash, @now, 1);
            SELECT AttemptCount FROM dbo.LoginAttempts WHERE AccountHash = @hash;
            COMMIT TRANSACTION;
            """;
        command.Parameters.Add("@hash", System.Data.SqlDbType.VarBinary, 32).Value = hash;
        return Convert.ToInt32(await command.ExecuteScalarAsync()) <= 5;
    }

    public async Task ClearAsync(string code)
    {
        await using var connection = OpenRmsConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.LoginAttempts WHERE AccountHash = @hash";
        command.Parameters.Add("@hash", System.Data.SqlDbType.VarBinary, 32).Value = Hash(code);
        await command.ExecuteNonQueryAsync();
    }

    private SqlConnection OpenRmsConnection()
    {
        var connectionString = db.Database.GetDbConnection().ConnectionString;
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.InitialCatalog, "RMS", StringComparison.Ordinal))
            throw new InvalidOperationException("Login guard is restricted to RMS");
        return new SqlConnection(connectionString);
    }

    private static byte[] Hash(string code) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToUpperInvariant()));
}
