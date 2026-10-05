using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using RMS_BACKEND.DTOs;
using RMS_BACKEND.Repositories;
using RMS_BACKEND.Services;
using RMS_BACKEND.Security;

namespace RMS_BACKEND.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IRequestStateMachineService _stateMachine;
        private readonly SessionService _sessions;
        private readonly ApplicationDbContext _db;
        private readonly LoginAttemptGuard _loginGuard;

        public AuthController(
            IEmployeeRepository employeeRepo,
            IRequestStateMachineService stateMachine,
            SessionService sessions,
            ApplicationDbContext db,
            LoginAttemptGuard loginGuard)
        {
            _employeeRepo = employeeRepo;
            _stateMachine = stateMachine;
            _sessions = sessions;
            _db = db;
            _loginGuard = loginGuard;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 50 ||
                    string.IsNullOrEmpty(request.Password))
                    return Unauthorized(new { message = "Invalid credentials" });
                if (!await _loginGuard.TryAcquireAsync(request.Code))
                    return StatusCode(429, new { message = "Too many login attempts; try again later" });
                var employee = await _employeeRepo.AuthenticateAsync(request.Code, request.Password);

                if (employee == null)
                    return Unauthorized(new { message = "Invalid credentials" });

                await _loginGuard.ClearAsync(request.Code);

                var isManager = employee.EmployeeRole == Models.EmployeeRole.Manager;
                var role = _stateMachine.DetermineUserRole(employee.DepartmentID, isManager);

                var response = new LoginResponseDto
                {
                    Id = employee.Id,
                    Code = employee.Code,
                    Name = employee.Name,
                    DepartmentID = employee.DepartmentID,
                    DepartmentName = employee.Department?.StatusName ?? "",
                    Role = role,
                    IsManager = isManager,
                    ManagerId = employee.ManagerId,
                    Token = await _sessions.IssueAsync(employee.Id)
                };

                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Login failed" });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (Guid.TryParse(User.FindFirstValue("session_id"), out var sessionId))
                await _sessions.RevokeAsync(sessionId);
            return NoContent();
        }

        [HttpGet("me")]
        public async Task<ActionResult<LoginResponseDto>> Me()
        {
            var employee = await _employeeRepo.GetByIdAsync(User.EmployeeId());
            if (employee is null || employee.IsDeleted) return Unauthorized();
            var isManager = employee.EmployeeRole == Models.EmployeeRole.Manager;
            return Ok(new LoginResponseDto
            {
                Id = employee.Id,
                Code = employee.Code,
                Name = employee.Name,
                DepartmentID = employee.DepartmentID,
                DepartmentName = employee.Department?.StatusName ?? "",
                Role = _stateMachine.DetermineUserRole(employee.DepartmentID, isManager),
                IsManager = isManager,
                ManagerId = employee.ManagerId
            });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword) ||
                request.NewPassword.Length < 9 || request.NewPassword.Length > 128)
                return BadRequest(new { message = "New password must be 9–128 characters" });

            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == User.EmployeeId() && !e.IsDeleted);
            if (employee is null) return Unauthorized();
            if (!string.Equals(employee.Password, request.CurrentPassword, StringComparison.Ordinal))
                return BadRequest(new { message = "Current password is incorrect" });
            if (request.CurrentPassword == request.NewPassword)
                return BadRequest(new { message = "New password must be different" });

            employee.Password = request.NewPassword;
            var now = DateTime.UtcNow;
            await using var transaction = await _db.Database.BeginTransactionAsync();
            await _db.AuthSessions.Where(s => s.EmployeeId == employee.Id && s.RevokedUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.RevokedUtc, now));
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return NoContent();
        }
    }
}
