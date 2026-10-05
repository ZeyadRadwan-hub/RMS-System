using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RMS_BACKEND.Data;
using RMS_BACKEND.Models;
using RMS_BACKEND.Services;

namespace RMS_BACKEND.Security;

public sealed class BearerSessionHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "RmsSession";
    private readonly ApplicationDbContext _db;
    private readonly IRequestStateMachineService _stateMachine;

    public BearerSessionHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ApplicationDbContext db,
        IRequestStateMachineService stateMachine) : base(options, logger, encoder)
    {
        _db = db;
        _stateMachine = stateMachine;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = header[7..].Trim();
        if (token.Length is < 40 or > 100 || token.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_')))
            return AuthenticateResult.Fail("Invalid session token");

        byte[] tokenBytes;
        try
        {
            tokenBytes = Convert.FromBase64String(token.Replace('-', '+').Replace('_', '/') +
                new string('=', (4 - token.Length % 4) % 4));
        }
        catch (FormatException)
        {
            return AuthenticateResult.Fail("Invalid session token");
        }

        if (tokenBytes.Length != 32)
            return AuthenticateResult.Fail("Invalid session token");

        var hash = SHA256.HashData(tokenBytes);
        var now = DateTime.UtcNow;
        var session = await _db.AuthSessions.AsNoTracking()
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.TokenHash == hash && s.RevokedUtc == null && s.ExpiresUtc > now);

        if (session?.Employee is not { IsDeleted: false } employee)
            return AuthenticateResult.Fail("Invalid or expired session");

        var role = _stateMachine.DetermineUserRole(employee.DepartmentID,
            employee.EmployeeRole == EmployeeRole.Manager);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("session_id", session.Id.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}
