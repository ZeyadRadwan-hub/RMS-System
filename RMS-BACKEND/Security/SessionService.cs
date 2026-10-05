using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using RMS_BACKEND.Models;

namespace RMS_BACKEND.Security;

public sealed class SessionService(ApplicationDbContext db)
{
    public async Task<string> IssueAsync(int employeeId)
    {
        var secret = RandomNumberGenerator.GetBytes(32);
        var now = DateTime.UtcNow;
        db.AuthSessions.Add(new AuthSession
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TokenHash = SHA256.HashData(secret),
            CreatedUtc = now,
            ExpiresUtc = now.AddHours(8)
        });
        await db.SaveChangesAsync();
        return Convert.ToBase64String(secret).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public async Task RevokeAsync(Guid sessionId)
    {
        var session = await db.AuthSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null || session.RevokedUtc is not null) return;
        session.RevokedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
