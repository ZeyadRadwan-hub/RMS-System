using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using RMS_BACKEND.Models;

namespace RMS_BACKEND.Services;

public sealed class TransactionRequestValidator(
    ApplicationDbContext db,
    ILeaveBalanceService balances,
    ILeaveCalculationService calculations)
{
    public async Task ValidateAsync(Employee employee, int typeId, DateTime start,
        DateTime end, int? substituteId, string? rationale, int? excludeTransactionId = null)
    {
        if (start == default || end == default || end.Date < start.Date)
            throw new ArgumentException("End date must be on or after start date");
        if (start.Date.Year < 2000 || end.Date.Year > 2100)
            throw new ArgumentException("Request dates are outside the supported range");
        if (string.IsNullOrWhiteSpace(rationale) || rationale.Length > 500)
            throw new ArgumentException("A reason of at most 500 characters is required");

        var type = await db.TransactionTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == typeId);
        if (type is null) throw new ArgumentException("Unknown leave type");

        if (substituteId.HasValue)
        {
            var substitute = await db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == substituteId.Value && !e.IsDeleted);
            if (substitute is null || substitute.Id == employee.Id ||
                substitute.DepartmentID != employee.DepartmentID ||
                substitute.EmployeeRole != EmployeeRole.Employee)
                throw new ArgumentException("Substitute must be an active colleague in the same department");
        }

        var startDay = start.Date;
        var endExclusive = end.Date.AddDays(1);
        var activeStatuses = new[]
        {
            TransactionStatus.Pending,
            TransactionStatus.PendingHR,
            TransactionStatus.ApprovedByHR
        };
        var overlap = await db.Transactions.AsNoTracking().AnyAsync(t =>
            t.EmployeeId == employee.Id && t.Id != excludeTransactionId &&
            activeStatuses.Contains(t.StatusID) &&
            t.StartDate < endExclusive && t.EndDate >= startDay);
        if (overlap) throw new ArgumentException("Request overlaps an existing active request");

        if (type.Sign >= 0) return;

        var proposed = new Transaction { StartDate = startDay, EndDate = end.Date };
        var requestedDays = -calculations.CalculateLeaveDays(proposed, type);
        var balance = await balances.GetLeaveBalanceAsync(employee.Id, end.Date);
        var pending = await db.Transactions.AsNoTracking()
            .Include(t => t.TransactionType)
            .Where(t => t.EmployeeId == employee.Id && t.Id != excludeTransactionId &&
                (t.StatusID == TransactionStatus.Pending || t.StatusID == TransactionStatus.PendingHR) &&
                t.StartDate < endExclusive)
            .ToListAsync();
        var reservedDays = pending.Sum(t =>
            Math.Max(0, -calculations.CalculateLeaveDays(t, t.TransactionType!)));
        if (requestedDays + reservedDays > balance.LeaveBalance + 0.00001)
            throw new ArgumentException("Insufficient available leave balance");
    }
}
