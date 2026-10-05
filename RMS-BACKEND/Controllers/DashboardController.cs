using Microsoft.AspNetCore.Mvc;
using RMS_BACKEND.DTOs;
using RMS_BACKEND.Services;
using RMS_BACKEND.Security;

namespace RMS_BACKEND.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Get dashboard statistics with filters
        /// </summary>
        [HttpPost("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats(
            [FromBody] DashboardFilterDto filter)
        {
            try
            {
                var result = await _dashboardService.GetDashboardStatsAsync(filter, User.EmployeeId(), User.RmsRole());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Get chart data (bar and pie charts) with filters
        /// </summary>
        [HttpPost("charts")]
        public async Task<ActionResult<ChartDataDto>> GetChartData(
            [FromBody] DashboardFilterDto filter)
        {
            try
            {
                var result = await _dashboardService.GetChartDataAsync(filter, User.EmployeeId(), User.RmsRole());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }
    }
}
