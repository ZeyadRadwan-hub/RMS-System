using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using RMS_BACKEND.DTOs;

namespace RMS_BACKEND.Services
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync(DashboardFilterDto filter, int employeeId, string role);
        Task<ChartDataDto> GetChartDataAsync(DashboardFilterDto filter, int employeeId, string role);
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRequestStateMachineService _stateMachine;

        public DashboardService(
            ApplicationDbContext context,
            IRequestStateMachineService stateMachine)
        {
            _context = context;
            _stateMachine = stateMachine;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(DashboardFilterDto filter, int employeeId, string role)
        {
            var query = BuildBaseQuery(filter, employeeId, role);

            var transactions = await query.ToListAsync();

            var stats = new DashboardStatsDto
            {
                TotalRequests = transactions.Count,
                PendingRequests = transactions.Count(t => t.StatusID == TransactionStatus.Pending ||
                    t.StatusID == TransactionStatus.PendingHR),
                ApprovedRequests = transactions.Count(t => t.StatusID == TransactionStatus.ApprovedByHR),
                RejectedRequests = transactions.Count(t => 
                    t.StatusID == TransactionStatus.RejectedByManager || 
                    t.StatusID == TransactionStatus.RejectedByHR),
                CancelledRequests = transactions.Count(t => t.StatusID == TransactionStatus.CancelledByEmployee)
            };

            // Group by Department
            if (filter.GroupBy == "Department" || string.IsNullOrEmpty(filter.GroupBy))
            {
                stats.DepartmentStats = transactions
                    .GroupBy(t => new { t.Employee!.DepartmentID, t.Employee.Department!.StatusName })
                    .Select(g => new DepartmentStatsDto
                    {
                        DepartmentID = g.Key.DepartmentID,
                        DepartmentName = g.Key.StatusName ?? "",
                        RequestCount = g.Count()
                    })
                    .OrderByDescending(d => d.RequestCount)
                    .ToList();
            }

            // Group by Employee
            if (filter.GroupBy == "Employee")
            {
                stats.EmployeeStats = transactions
                    .GroupBy(t => new { t.EmployeeId, t.Employee!.Code, t.Employee.Name })
                    .Select(g => new EmployeeStatsDto
                    {
                        EmployeeId = g.Key.EmployeeId,
                        EmployeeCode = g.Key.Code,
                        EmployeeName = g.Key.Name,
                        RequestCount = g.Count()
                    })
                    .OrderByDescending(e => e.RequestCount)
                    .ToList();
            }

            // Status breakdown
            stats.StatusStats = transactions
                .GroupBy(t => new { t.StatusID, t.Status!.StatusName })
                .Select(g => new StatusStatsDto
                {
                    StatusID = g.Key.StatusID,
                    StatusName = g.Key.StatusName ?? "",
                    RequestCount = g.Count()
                })
                .OrderBy(s => s.StatusID)
                .ToList();

            return stats;
        }

        public async Task<ChartDataDto> GetChartDataAsync(DashboardFilterDto filter, int employeeId, string role)
        {
            var query = BuildBaseQuery(filter, employeeId, role);
            var transactions = await query.ToListAsync();

            var chartData = new ChartDataDto();
            var totalCount = transactions.Count;

            // Determine grouping
            var groupBy = filter.GroupBy ?? "Department";

            if (groupBy == "Department")
            {
                var departmentGroups = transactions
                    .GroupBy(t => new { t.Employee!.DepartmentID, t.Employee.Department!.StatusName })
                    .Select(g => new
                    {
                        Label = g.Key.StatusName ?? "Unknown",
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                chartData.BarChartData = departmentGroups
                    .Select(d => new BarChartDataDto
                    {
                        Label = d.Label,
                        Value = d.Count
                    })
                    .ToList();

                chartData.PieChartData = departmentGroups
                    .Select(d => new PieChartDataDto
                    {
                        Label = d.Label,
                        Value = d.Count,
                        Percentage = totalCount > 0 ? Math.Round((double)d.Count / totalCount * 100, 2) : 0
                    })
                    .ToList();
            }
            else if (groupBy == "Employee")
            {
                var employeeGroups = transactions
                    .GroupBy(t => new { t.EmployeeId, t.Employee!.Code, t.Employee.Name })
                    .Select(g => new
                    {
                        Label = $"{g.Key.Code} - {g.Key.Name}",
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10) // Top 10 employees
                    .ToList();

                chartData.BarChartData = employeeGroups
                    .Select(e => new BarChartDataDto
                    {
                        Label = e.Label,
                        Value = e.Count
                    })
                    .ToList();

                chartData.PieChartData = employeeGroups
                    .Select(e => new PieChartDataDto
                    {
                        Label = e.Label,
                        Value = e.Count,
                        Percentage = totalCount > 0 ? Math.Round((double)e.Count / totalCount * 100, 2) : 0
                    })
                    .ToList();
            }
            else // Status
            {
                var statusGroups = transactions
                    .GroupBy(t => new { t.StatusID, t.Status!.StatusName })
                    .Select(g => new
                    {
                        Label = g.Key.StatusName ?? "Unknown",
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                chartData.BarChartData = statusGroups
                    .Select(s => new BarChartDataDto
                    {
                        Label = s.Label,
                        Value = s.Count
                    })
                    .ToList();

                chartData.PieChartData = statusGroups
                    .Select(s => new PieChartDataDto
                    {
                        Label = s.Label,
                        Value = s.Count,
                        Percentage = totalCount > 0 ? Math.Round((double)s.Count / totalCount * 100, 2) : 0
                    })
                    .ToList();
            }

            return chartData;
        }

        private IQueryable<Models.Transaction> BuildBaseQuery(DashboardFilterDto filter, int employeeId, string role)
        {
            var query = _context.Transactions
                .Include(t => t.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(t => t.Status)
                .AsQueryable();

            // Role-based filtering
            if (role == "Employee")
            {
                // Regular employees only see their own requests
                query = query.Where(t => t.EmployeeId == employeeId);
            }
            else if (role == "Manager")
            {
                // Managers see their team's requests
                var subordinateIds = _context.Employees
                    .Where(e => e.ManagerId == employeeId)
                    .Select(e => e.Id)
                    .ToList();

                query = query.Where(t => subordinateIds.Contains(t.EmployeeId));
            }
            else if (role is not ("HR" or "Board"))
            {
                throw new UnauthorizedAccessException("Unknown role");
            }
            // HR and Board see all requests (no additional filtering)

            // Apply filters
            if (filter.StatusID.HasValue)
                query = query.Where(t => t.StatusID == filter.StatusID.Value);

            if (filter.DepartmentID.HasValue)
                query = query.Where(t => t.Employee!.DepartmentID == filter.DepartmentID.Value);

            if (filter.EmployeeId.HasValue)
                query = query.Where(t => t.EmployeeId == filter.EmployeeId.Value);

            if (filter.StartDate.HasValue)
                query = query.Where(t => t.StartDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(t => t.EndDate <= filter.EndDate.Value);

            return query;
        }
    }
}
