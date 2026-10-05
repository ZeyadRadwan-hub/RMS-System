using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RMS_BACKEND.Data;
using RMS_BACKEND.Models;

namespace RMS_BACKEND.Repositories
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByIdAsync(int id);
        Task<Employee?> GetByCodeAsync(string code);
        Task<Employee?> AuthenticateAsync(string code, string password);
        Task<List<Employee>> GetAllAsync();
        Task<List<Employee>> GetByDepartmentAsync(int departmentId);
        Task<List<Employee>> GetSubordinatesAsync(int managerId);
        Task<Employee> CreateAsync(Employee employee);
        Task<Employee> UpdateAsync(Employee employee);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> CodeExistsAsync(string code);
        Task<int> GetNextIdAsync();
    }

    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Employee> _passwordHasher;

        public EmployeeRepository(ApplicationDbContext context, IPasswordHasher<Employee> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.EmployeeLevel)
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Employee?> GetByCodeAsync(string code)
        {
            return await _context.Employees
                .Include(e => e.EmployeeLevel)
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .FirstOrDefaultAsync(e => e.Code == code);
        }

        public async Task<Employee?> AuthenticateAsync(string code, string password)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrEmpty(password)) return null;
            var employee = await _context.Employees
                .Include(e => e.EmployeeLevel)
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .FirstOrDefaultAsync(e => e.Code == code && !e.IsDeleted);
            if (employee is null || !employee.Password.StartsWith("AQAAAA", StringComparison.Ordinal))
                return null;
            try
            {
                var result = _passwordHasher.VerifyHashedPassword(employee, employee.Password, password);
                return result == PasswordVerificationResult.Failed ? null : employee;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public async Task<List<Employee>> GetAllAsync()
        {
            return await _context.Employees
                .Include(e => e.EmployeeLevel)
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .ToListAsync();
        }

        public async Task<List<Employee>> GetByDepartmentAsync(int departmentId)
        {
            return await _context.Employees
                .Include(e => e.EmployeeLevel)
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .Where(e => e.DepartmentID == departmentId)
                .ToListAsync();
        }

        public async Task<List<Employee>> GetSubordinatesAsync(int managerId)
        {
            return await _context.Employees
                .Include(e => e.EmployeeLevel)
                .Include(e => e.Department)
                .Where(e => e.ManagerId == managerId)
                .ToListAsync();
        }

        public async Task<Employee> CreateAsync(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return employee;
        }

        public async Task<Employee> UpdateAsync(Employee employee)
        {
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
            return employee;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null || employee.IsDeleted)
                return false;

            employee.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Employees.AnyAsync(e => e.Id == id);
        }

        public async Task<bool> CodeExistsAsync(string code)
        {
            return await _context.Employees.AnyAsync(e => e.Code == code);
        }

        public Task<int> GetNextIdAsync() => SequenceIdAllocator.NextEmployeeIdAsync(_context);
    }
}
