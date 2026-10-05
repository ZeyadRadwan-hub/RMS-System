namespace RMS_BACKEND.DTOs
{
    public class LeaveBalanceDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime DateOfEmployment { get; set; }
        public int MonthsOfService { get; set; }
        public string EmployeeLevel { get; set; } = string.Empty;
        public int AnnualLeaveEntitlement { get; set; }
        public double MonthlyAccrual { get; set; }
        public double TotalAccruedLeave { get; set; }
        public double LeaveUsed { get; set; }
        public double LeaveBalance { get; set; }
        public double CarryoverFromPreviousYear { get; set; }
        public bool IsInProbation { get; set; }
        public DateTime CalculationDate { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class LeaveBalanceReportRequestDto
    {
        public DateTime? AsOfDate { get; set; }
        public int? EmployeeId { get; set; }
    }
}
