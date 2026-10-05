using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using RMS_BACKEND.DTOs;
using RMS_BACKEND.Models;
using RMS_BACKEND.Repositories;

namespace RMS_BACKEND.Services
{
    public interface ILeaveBalanceService
    {
        Task<LeaveBalanceDto> GetLeaveBalanceAsync(int employeeId, DateTime? asOfDate = null);
        Task<List<LeaveBalanceDto>> GetLeaveBalancesForDepartmentAsync(int departmentId, DateTime? asOfDate = null);
        Task<List<LeaveBalanceDto>> GetLeaveBalancesForManagerAsync(int managerId, DateTime? asOfDate = null);
        Task<List<LeaveBalanceDto>> GetAllLeaveBalancesAsync(DateTime? asOfDate = null);
    }

    public class LeaveBalanceService : ILeaveBalanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILeaveCalculationService _calculationService;

        public LeaveBalanceService(
            ApplicationDbContext context,
            ILeaveCalculationService calculationService)
        {
            _context = context;
            _calculationService = calculationService;
        }

        public async Task<LeaveBalanceDto> GetLeaveBalanceAsync(int employeeId, DateTime? asOfDate = null)
        {
            var balances = await GetBalancesForEmployeesAsync(
                _context.Employees.Where(e => e.Id == employeeId), asOfDate);
            return balances.SingleOrDefault()
                ?? throw new KeyNotFoundException("Employee not found");
        }

        public async Task<List<LeaveBalanceDto>> GetLeaveBalancesForDepartmentAsync(int departmentId, DateTime? asOfDate = null)
        {
            return await GetBalancesForEmployeesAsync(
                _context.Employees.Where(e => e.DepartmentID == departmentId), asOfDate);
        }

        public async Task<List<LeaveBalanceDto>> GetLeaveBalancesForManagerAsync(int managerId, DateTime? asOfDate = null)
        {
            return await GetBalancesForEmployeesAsync(
                _context.Employees.Where(e => e.ManagerId == managerId), asOfDate);
        }

        public async Task<List<LeaveBalanceDto>> GetAllLeaveBalancesAsync(DateTime? asOfDate = null)
        {
            return await GetBalancesForEmployeesAsync(_context.Employees, asOfDate);
        }

        private async Task<List<LeaveBalanceDto>> GetBalancesForEmployeesAsync(
            IQueryable<Employee> employeeQuery, DateTime? asOfDate)
        {
            var calculationDate = asOfDate ?? DateTime.Now;
            var employees = await employeeQuery
                .Include(e => e.EmployeeLevel)
                .Include(e => e.Department)
                .ToListAsync();
            if (employees.Count == 0) return new List<LeaveBalanceDto>();

            var employeeIds = employees.Select(e => e.Id).ToArray();
            var yearStart = new DateTime(calculationDate.Year, 1, 1);
            var asOfDay = calculationDate.Date;
            var asOfExclusive = asOfDay.AddDays(1);
            var transactions = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.TransactionType)
                .Where(t => employeeIds.Contains(t.EmployeeId) &&
                            t.StartDate < asOfExclusive &&
                            t.EndDate >= yearStart &&
                            t.StatusID == TransactionStatus.ApprovedByHR)
                .ToListAsync();
            var transactionsByEmployee = transactions
                .GroupBy(t => t.EmployeeId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Transaction>)g.ToList());

            return employees.Select(employee => BuildBalance(
                employee,
                transactionsByEmployee.GetValueOrDefault(employee.Id) ?? Array.Empty<Transaction>(),
                calculationDate, yearStart, asOfDay)).ToList();
        }

        private LeaveBalanceDto BuildBalance(Employee employee,
            IReadOnlyList<Transaction> transactions, DateTime calculationDate,
            DateTime yearStart, DateTime asOfDay)
        {
            var monthsOfService = CalculateMonthsOfService(employee.DateOfEmployment, calculationDate);
            var isInProbation = _calculationService.IsInProbationPeriod(employee.DateOfEmployment, calculationDate);
            var annualLeaveEntitlement = employee.EmployeeLevel?.AnnualLeaveEntitlement ?? 0;
            var monthlyAccrual = annualLeaveEntitlement / 12.0;
            var earnedMonths = Math.Min(calculationDate.Month, Math.Max(0, monthsOfService - 6));
            var earnedDays = earnedMonths * monthlyAccrual;
            double totalApprovedLeaveDays = 0;
            double totalBonusDays = 0;

            foreach (var transaction in transactions)
            {
                if (transaction.TransactionType is null) continue;
                var clippedStart = transaction.StartDate.Date < yearStart ? yearStart : transaction.StartDate.Date;
                var clippedEnd = transaction.EndDate.Date > asOfDay ? asOfDay : transaction.EndDate.Date;
                var clipped = new Transaction { StartDate = clippedStart, EndDate = clippedEnd };
                var days = _calculationService.CalculateLeaveDays(clipped, transaction.TransactionType);
                if (days < 0) totalApprovedLeaveDays -= days;
                if (days > 0) totalBonusDays += days;
            }

            static double RoundToHalfDay(double value) =>
                Math.Round(value * 2, MidpointRounding.AwayFromZero) / 2.0;
            var roundedEarnedDays = RoundToHalfDay(earnedDays);
            var roundedLeaveUsed = RoundToHalfDay(totalApprovedLeaveDays);

            return new LeaveBalanceDto
            {
                EmployeeId = employee.Id,
                EmployeeCode = employee.Code,
                EmployeeName = employee.Name,
                DateOfEmployment = employee.DateOfEmployment,
                MonthsOfService = monthsOfService,
                EmployeeLevel = employee.EmployeeLevel?.LevelName ?? "",
                AnnualLeaveEntitlement = annualLeaveEntitlement,
                MonthlyAccrual = monthlyAccrual,
                TotalAccruedLeave = roundedEarnedDays,
                LeaveUsed = roundedLeaveUsed,
                LeaveBalance = roundedEarnedDays + RoundToHalfDay(totalBonusDays) - roundedLeaveUsed,
                CarryoverFromPreviousYear = 0,
                IsInProbation = isInProbation,
                CalculationDate = calculationDate,
                DepartmentName = employee.Department?.StatusName ?? ""
            };
        }

        private int CalculateMonthsOfService(DateTime dateOfEmployment, DateTime asOfDate)
        {
            var years = asOfDate.Year - dateOfEmployment.Year;
            var months = asOfDate.Month - dateOfEmployment.Month;
            var totalMonths = (years * 12) + months;

            if (asOfDate.Day < dateOfEmployment.Day)
                totalMonths--;

            return totalMonths > 0 ? totalMonths : 0;
        }
    }
}
