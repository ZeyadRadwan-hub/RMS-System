using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RMS_BACKEND.DTOs;
using RMS_BACKEND.Services;
using RMS_BACKEND.Security;
using RMS_BACKEND.Repositories;

namespace RMS_BACKEND.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveBalanceController : ControllerBase
    {
        private readonly ILeaveBalanceService _leaveBalanceService;
        private readonly IEmployeeRepository _employees;

        public LeaveBalanceController(ILeaveBalanceService leaveBalanceService, IEmployeeRepository employees)
        {
            _leaveBalanceService = leaveBalanceService;
            _employees = employees;
        }

        /// <summary>
        /// Get leave balance for a specific employee
        /// </summary>
        [HttpGet("{employeeId}")]
        public async Task<ActionResult<LeaveBalanceDto>> GetLeaveBalance(
            int employeeId,
            [FromQuery] DateTime? asOfDate = null)
        {
            if (!User.IsOrganizationReader() && employeeId != User.EmployeeId())
            {
                var target = await _employees.GetByIdAsync(employeeId);
                if (!(User.IsInRole("Manager") && target?.ManagerId == User.EmployeeId()))
                    return Forbid();
            }
            try
            {
                var result = await _leaveBalanceService.GetLeaveBalanceAsync(employeeId, asOfDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Get my leave balance (Employee)
        /// </summary>
        [HttpGet("my-balance")]
        public async Task<ActionResult<LeaveBalanceDto>> GetMyLeaveBalance(
            [FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var result = await _leaveBalanceService.GetLeaveBalanceAsync(User.EmployeeId(), asOfDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Get leave balances for my team (Manager)
        /// </summary>
        [HttpGet("team-balances")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<List<LeaveBalanceDto>>> GetTeamLeaveBalances(
            [FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var result = await _leaveBalanceService.GetLeaveBalancesForManagerAsync(User.EmployeeId(), asOfDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Get leave balances for a department (HR/Board)
        /// </summary>
        [HttpGet("department/{departmentId}")]
        [Authorize(Roles = "HR,Board")]
        public async Task<ActionResult<List<LeaveBalanceDto>>> GetDepartmentLeaveBalances(
            int departmentId,
            [FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var result = await _leaveBalanceService.GetLeaveBalancesForDepartmentAsync(departmentId, asOfDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Get all leave balances (HR/Board)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "HR,Board")]
        public async Task<ActionResult<List<LeaveBalanceDto>>> GetAllLeaveBalances(
            [FromQuery] DateTime? asOfDate = null)
        {
            try
            {
                var result = await _leaveBalanceService.GetAllLeaveBalancesAsync(asOfDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }
    }
}
