using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using RMS_BACKEND.DTOs;
using RMS_BACKEND.Models;
using RMS_BACKEND.Repositories;
using RMS_BACKEND.Security;

namespace RMS_BACKEND.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IPasswordHasher<Employee> _passwordHasher;
        private readonly ApplicationDbContext _db;

        public EmployeesController(IEmployeeRepository employeeRepo, IPasswordHasher<Employee> passwordHasher,
            ApplicationDbContext db)
        {
            _employeeRepo = employeeRepo;
            _passwordHasher = passwordHasher;
            _db = db;
        }

        /// <summary>
        /// Get all employees (HR only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "HR")]
        public async Task<ActionResult<List<EmployeeListDto>>> GetAllEmployees()
        {
            try
            {
                var employees = await _employeeRepo.GetAllAsync();
                var result = employees.Select(e => new EmployeeListDto
                {
                    Id = e.Id,
                    Code = e.Code,
                    Name = e.Name,
                    DateOfEmployment = e.DateOfEmployment,
                    EmployeeRole = e.EmployeeRole.ToString(),
                    EmployeeLevel = e.EmployeeLevel?.LevelName ?? "",
                    EmployeeLevelId = e.EmployeeLevelId,
                    AnnualLeaveEntitlement = e.EmployeeLevel?.AnnualLeaveEntitlement ?? 0,
                    ManagerId = e.ManagerId,
                    ManagerName = e.Manager?.Name,
                    DepartmentID = e.DepartmentID,
                    DepartmentName = e.Department?.StatusName ?? "",
                    IsActive = !e.IsDeleted,
                    IsManager = e.EmployeeRole == EmployeeRole.Manager
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Get employee by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "HR")]
        public async Task<ActionResult<EmployeeListDto>> GetEmployee(int id)
        {
            try
            {
                var employee = await _employeeRepo.GetByIdAsync(id);
                if (employee == null)
                    return NotFound(new { message = "Employee not found" });

                var previousManagerId = employee.ManagerId;

                var result = new EmployeeListDto
                {
                    Id = employee.Id,
                    Code = employee.Code,
                    Name = employee.Name,
                    DateOfEmployment = employee.DateOfEmployment,
                    EmployeeRole = employee.EmployeeRole.ToString(),
                    EmployeeLevel = employee.EmployeeLevel?.LevelName ?? "",
                    EmployeeLevelId = employee.EmployeeLevelId,
                    AnnualLeaveEntitlement = employee.EmployeeLevel?.AnnualLeaveEntitlement ?? 0,
                    ManagerId = employee.ManagerId,
                    ManagerName = employee.Manager?.Name,
                    DepartmentID = employee.DepartmentID,
                    DepartmentName = employee.Department?.StatusName ?? "",
                    IsActive = !employee.IsDeleted,
                    IsManager = employee.EmployeeRole == EmployeeRole.Manager
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Create new employee (HR only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<ActionResult<EmployeeListDto>> CreateEmployee([FromBody] CreateEmployeeDto request)
        {
            try
            {
                var inputError = await ValidateEmployeeInputAsync(request.Code, request.Name,
                    request.DateOfEmployment, request.EmployeeRole, request.EmployeeLevelId, request.DepartmentID);
                if (inputError is not null) return BadRequest(new { message = inputError });
                // Check if code already exists
                if (await _employeeRepo.CodeExistsAsync(request.Code))
                    return BadRequest(new { message = "Employee code already exists" });

                if (request.ManagerId.HasValue &&
                    !await _db.Employees.AnyAsync(e => e.Id == request.ManagerId && !e.IsDeleted))
                    return BadRequest(new { message = "Manager must be an active employee" });

                var nextId = await _employeeRepo.GetNextIdAsync();

                var employee = new Employee
                {
                    Id = nextId,
                    Code = request.Code,
                    Name = request.Name,
                    Password = string.Empty,
                    DateOfEmployment = request.DateOfEmployment,
                    EmployeeRole = (EmployeeRole)request.EmployeeRole,
                    EmployeeLevelId = request.EmployeeLevelId,
                    ManagerId = request.ManagerId,
                    DepartmentID = request.DepartmentID
                };

                if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length is < 12 or > 128)
                    return BadRequest(new { message = "Password must contain 12–128 characters" });
                employee.Password = _passwordHasher.HashPassword(employee, request.Password);

                await using var dbTransaction = await _db.Database.BeginTransactionAsync();
                var created = await _employeeRepo.CreateAsync(employee);
                await UpdateManagerStatus(request.ManagerId);
                await dbTransaction.CommitAsync();
                
                var result = await _employeeRepo.GetByIdAsync(created.Id);

                return Ok(new EmployeeListDto
                {
                    Id = result!.Id,
                    Code = result.Code,
                    Name = result.Name,
                    DateOfEmployment = result.DateOfEmployment,
                    EmployeeRole = result.EmployeeRole.ToString(),
                    EmployeeLevel = result.EmployeeLevel?.LevelName ?? "",
                    EmployeeLevelId = result.EmployeeLevelId,
                    AnnualLeaveEntitlement = result.EmployeeLevel?.AnnualLeaveEntitlement ?? 0,
                    ManagerId = result.ManagerId,
                    ManagerName = result.Manager?.Name,
                    DepartmentID = result.DepartmentID,
                    DepartmentName = result.Department?.StatusName ?? "",
                    IsActive = !result.IsDeleted,
                    IsManager = result.EmployeeRole == EmployeeRole.Manager
                });
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Update employee (HR only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "HR")]
        public async Task<ActionResult<EmployeeListDto>> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto request)
        {
            try
            {
                var employee = await _employeeRepo.GetByIdAsync(id);
                if (employee == null)
                    return NotFound(new { message = "Employee not found" });

                var previousManagerId = employee.ManagerId;

                var inputError = await ValidateEmployeeInputAsync(request.Code, request.Name,
                    request.DateOfEmployment, request.EmployeeRole, request.EmployeeLevelId, request.DepartmentID);
                if (inputError is not null) return BadRequest(new { message = inputError });
                if (await _db.Employees.AnyAsync(e => e.Code == request.Code && e.Id != id))
                    return BadRequest(new { message = "Employee code already exists" });

                if (request.ManagerId == id)
                    return BadRequest(new { message = "An employee cannot manage themself" });
                if (request.ManagerId.HasValue &&
                    !await _db.Employees.AnyAsync(e => e.Id == request.ManagerId && !e.IsDeleted))
                    return BadRequest(new { message = "Manager must be an active employee" });
                if (request.ManagerId.HasValue && await WouldCreateManagerCycleAsync(id, request.ManagerId.Value))
                    return BadRequest(new { message = "Manager assignment would create a cycle" });
                if (request.EmployeeRole == 0 &&
                    await _db.Employees.AnyAsync(e => e.ManagerId == id && !e.IsDeleted))
                    return BadRequest(new { message = "A manager with active direct reports cannot be demoted" });

                employee.Code = request.Code;
                employee.Name = request.Name;
                employee.DateOfEmployment = request.DateOfEmployment;
                employee.EmployeeRole = (EmployeeRole)request.EmployeeRole;
                employee.EmployeeLevelId = request.EmployeeLevelId;
                employee.ManagerId = request.ManagerId;
                employee.DepartmentID = request.DepartmentID;

                await using var dbTransaction = await _db.Database.BeginTransactionAsync();
                var updated = await _employeeRepo.UpdateAsync(employee);
                await UpdateManagerStatus(previousManagerId);
                await UpdateManagerStatus(request.ManagerId);
                await dbTransaction.CommitAsync();
                
                var result = await _employeeRepo.GetByIdAsync(updated.Id);

                return Ok(new EmployeeListDto
                {
                    Id = result!.Id,
                    Code = result.Code,
                    Name = result.Name,
                    DateOfEmployment = result.DateOfEmployment,
                    EmployeeRole = result.EmployeeRole.ToString(),
                    EmployeeLevel = result.EmployeeLevel?.LevelName ?? "",
                    EmployeeLevelId = result.EmployeeLevelId,
                    AnnualLeaveEntitlement = result.EmployeeLevel?.AnnualLeaveEntitlement ?? 0,
                    ManagerId = result.ManagerId,
                    ManagerName = result.Manager?.Name,
                    DepartmentID = result.DepartmentID,
                    DepartmentName = result.Department?.StatusName ?? "",
                    IsActive = !result.IsDeleted,
                    IsManager = result.EmployeeRole == EmployeeRole.Manager
                });
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Soft delete employee (HR only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "HR")]
        public async Task<ActionResult> DeleteEmployee(int id)
        {
            try
            {
                var result = await _employeeRepo.DeleteAsync(id);
                if (!result)
                    return NotFound(new { message = "Employee not found" });

                return Ok(new { message = "Employee deleted successfully" });
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Recalculate manager status for all employees (HR only)
        /// </summary>
        [HttpPost("recalculate-managers")]
        [Authorize(Roles = "HR")]
        public async Task<ActionResult> RecalculateAllManagers()
        {
            try
            {
                var allEmployees = await _employeeRepo.GetAllAsync();
                
                // Get all unique manager IDs
                var managerIds = allEmployees
                    .Where(e => e.ManagerId != null)
                    .Select(e => e.ManagerId!.Value)
                    .Distinct()
                    .ToList();
                
                int updatedCount = 0;
                
                // Update each manager
                foreach (var managerId in managerIds)
                {
                    var manager = allEmployees.FirstOrDefault(e => e.Id == managerId);
                    if (manager != null && manager.EmployeeRole != EmployeeRole.Manager)
                    {
                        manager.EmployeeRole = EmployeeRole.Manager;
                        await _employeeRepo.UpdateAsync(manager);
                        updatedCount++;
                    }
                }
                
                return Ok(new { 
                    message = $"Successfully updated {updatedCount} employees to Manager role",
                    updatedCount = updatedCount,
                    totalManagers = managerIds.Count
                });
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Helper method to update manager status for an employee
        /// </summary>
        private async Task UpdateManagerStatus(int? managerId)
        {
            if (managerId == null) return;
            var manager = await _employeeRepo.GetByIdAsync(managerId.Value);
            if (manager is null || manager.IsDeleted)
                throw new ArgumentException("Manager must be an active employee");
            var hasSubordinates = await _db.Employees.AnyAsync(e => e.ManagerId == managerId.Value && !e.IsDeleted);
            var expectedRole = hasSubordinates ? EmployeeRole.Manager : EmployeeRole.Employee;
            if (manager.EmployeeRole != expectedRole)
            {
                manager.EmployeeRole = expectedRole;
                await _employeeRepo.UpdateAsync(manager);
            }
        }

        private async Task<bool> WouldCreateManagerCycleAsync(int employeeId, int managerId)
        {
            var visited = new HashSet<int>();
            int? currentId = managerId;
            while (currentId.HasValue)
            {
                if (currentId.Value == employeeId || !visited.Add(currentId.Value) || visited.Count >= 100)
                    return true;
                currentId = await _db.Employees.AsNoTracking()
                    .Where(e => e.Id == currentId.Value).Select(e => e.ManagerId).SingleOrDefaultAsync();
            }
            return false;
        }

        private async Task<string?> ValidateEmployeeInputAsync(string? code, string? name,
            DateTime employed, short role, int levelId, int departmentId)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length > 50 ||
                string.IsNullOrWhiteSpace(name) || name.Length > 200)
                return "Employee code and name are required and must fit their field limits";
            if (role is not (0 or 1)) return "Unknown employee role";
            if (employed == default || employed.Date > DateTime.Today)
                return "Employment date must not be in the future";
            if (!await _db.EmployeeLevels.AnyAsync(l => l.Id == levelId))
                return "Unknown employee level";
            if (!await _db.Statuses.AnyAsync(s => s.Id == departmentId && s.StatusType == "Department"))
                return "Unknown department";
            return null;
        }

        [HttpGet("substitutes")]
        public async Task<ActionResult> GetSubstitutes()
        {
            var actor = await _employeeRepo.GetByIdAsync(User.EmployeeId());
            if (actor is null) return Unauthorized();
            var candidates = await _employeeRepo.GetByDepartmentAsync(actor.DepartmentID);
            return Ok(candidates
                .Where(e => !e.IsDeleted && e.Id != actor.Id && e.EmployeeRole == EmployeeRole.Employee)
                .Select(e => new { e.Id, e.Name, e.DepartmentID, EmployeeRole = e.EmployeeRole.ToString() }));
        }
    }
}
