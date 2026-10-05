using RMS_BACKEND.Models;

namespace RMS_BACKEND.Services
{
    public interface ILeaveCalculationService
    {
        double CalculateLeaveDays(Transaction transaction, TransactionType transactionType);
        double CalculateMonthlyAccrual(int annualLeave);
        double CalculateTotalAccruedLeave(DateTime dateOfEmployment, int annualLeave, DateTime asOfDate);
        bool IsInProbationPeriod(DateTime dateOfEmployment, DateTime asOfDate);
        double CalculateLeaveBalance(int employeeId, DateTime asOfDate);
    }

    public class LeaveCalculationService : ILeaveCalculationService
    {
        /// <summary>
        /// Calculates leave days using formula: Unit × Sign × (EndDate - StartDate).Days
        /// </summary>
        public double CalculateLeaveDays(Transaction transaction, TransactionType transactionType)
        {
            if (transaction == null || transactionType == null)
                return 0;

            var daysDifference = (transaction.EndDate.Date - transaction.StartDate.Date).Days + 1;
            var calculatedDays = (double)transactionType.Unit * transactionType.Sign * daysDifference;

            return calculatedDays;
        }

        /// <summary>
        /// Calculates monthly accrual: Total Annual Leave / 12
        /// </summary>
        public double CalculateMonthlyAccrual(int annualLeave)
        {
            return Math.Round((double)annualLeave / 12, 2);
        }

        /// <summary>
        /// Calculates total accrued leave based on months of service
        /// New employees: no accrual first 6 months
        /// </summary>
        public double CalculateTotalAccruedLeave(DateTime dateOfEmployment, int annualLeave, DateTime asOfDate)
        {
            var monthsOfService = CalculateMonthsOfService(dateOfEmployment, asOfDate);

            // No accrual for first 6 months (probation period)
            if (monthsOfService < 6)
                return 0;

            var monthlyAccrual = CalculateMonthlyAccrual(annualLeave);
            var accruedMonths = monthsOfService - 6; // Exclude probation period

            return Math.Round(monthlyAccrual * accruedMonths, 2);
        }

        /// <summary>
        /// Checks if employee is in probation period (first 6 months)
        /// </summary>
        public bool IsInProbationPeriod(DateTime dateOfEmployment, DateTime asOfDate)
        {
            var monthsOfService = CalculateMonthsOfService(dateOfEmployment, asOfDate);
            return monthsOfService < 6;
        }

        /// <summary>
        /// Calculates current leave balance for an employee
        /// This is a placeholder - actual implementation will query transactions
        /// </summary>
        public double CalculateLeaveBalance(int employeeId, DateTime asOfDate)
        {
            // This will be implemented in the repository/service layer
            // where we have access to the database context
            throw new NotImplementedException("Use LeaveBalanceService for full calculation");
        }

        private int CalculateMonthsOfService(DateTime dateOfEmployment, DateTime asOfDate)
        {
            var years = asOfDate.Year - dateOfEmployment.Year;
            var months = asOfDate.Month - dateOfEmployment.Month;
            var totalMonths = (years * 12) + months;

            // Adjust if the day hasn't been reached yet in the current month
            if (asOfDate.Day < dateOfEmployment.Day)
                totalMonths--;

            return totalMonths > 0 ? totalMonths : 0;
        }
    }
}
