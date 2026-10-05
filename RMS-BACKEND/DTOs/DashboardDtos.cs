namespace RMS_BACKEND.DTOs
{
    public class DashboardFilterDto
    {
        public int? StatusID { get; set; }
        public int? DepartmentID { get; set; }
        public int? EmployeeId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? GroupBy { get; set; } // "Department" or "Employee"
    }

    public class DashboardStatsDto
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int CancelledRequests { get; set; }
        public List<DepartmentStatsDto> DepartmentStats { get; set; } = new();
        public List<EmployeeStatsDto> EmployeeStats { get; set; } = new();
        public List<StatusStatsDto> StatusStats { get; set; } = new();
    }

    public class DepartmentStatsDto
    {
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int RequestCount { get; set; }
    }

    public class EmployeeStatsDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public int RequestCount { get; set; }
    }

    public class StatusStatsDto
    {
        public int StatusID { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int RequestCount { get; set; }
    }

    public class ChartDataDto
    {
        public List<BarChartDataDto> BarChartData { get; set; } = new();
        public List<PieChartDataDto> PieChartData { get; set; } = new();
    }

    public class BarChartDataDto
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class PieChartDataDto
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        public double Percentage { get; set; }
    }
}
