using System.Security.Claims;

namespace RMS_BACKEND.Security;

public static class ActorClaims
{
    public static int EmployeeId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated employee ID missing"));

    public static string RmsRole(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role)
            ?? throw new InvalidOperationException("Authenticated role missing");

    public static bool IsOrganizationReader(this ClaimsPrincipal user) =>
        user.IsInRole("HR") || user.IsInRole("Board");
}
