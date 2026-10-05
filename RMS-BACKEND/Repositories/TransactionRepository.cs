using Microsoft.EntityFrameworkCore;
using RMS_BACKEND.Data;
using RMS_BACKEND.Models;

namespace RMS_BACKEND.Repositories
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(int id);
        Task<PagedTransactions> GetAllAsync(int skip, int take);
        Task<PagedTransactions> GetByEmployeeIdAsync(int employeeId, int skip, int take);
        Task<List<Transaction>> GetByStatusAsync(int statusId);
        Task<List<Transaction>> GetByDepartmentAsync(int departmentId);
        Task<PagedTransactions> GetByManagerAsync(int managerId, int skip, int take);
        Task<PagedTransactions> GetFilteredAsync(int? statusId, int? departmentId, int? employeeId, DateTime? startDate, DateTime? endDate, int requestingEmployeeId, string role, int skip, int take);
        Task<Transaction> CreateAsync(Transaction transaction);
        Task<Transaction> UpdateAsync(Transaction transaction);
        Task<bool> DeleteAsync(int id);
        Task<int> GetNextIdAsync();
    }

    public sealed record PagedTransactions(List<Transaction> Items, int TotalCount);

    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public TransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Transaction?> GetByIdAsync(int id)
        {
            return await _context.Transactions
                .Include(t => t.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(t => t.Employee)
                    .ThenInclude(e => e!.EmployeeLevel)
                .Include(t => t.SubstituteEmployee)
                .Include(t => t.TransactionType)
                .Include(t => t.Status)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<PagedTransactions> GetAllAsync(int skip, int take)
        {
            var query = _context.Transactions
                .Include(t => t.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(t => t.Employee)
                    .ThenInclude(e => e!.EmployeeLevel)
                .Include(t => t.SubstituteEmployee)
                .Include(t => t.TransactionType)
                .Include(t => t.Status)
                .OrderByDescending(t => t.CreationDate);
            var totalCount = await query.CountAsync();
            var items = await query.Skip(skip).Take(take).ToListAsync();
            return new PagedTransactions(items, totalCount);
        }

        public async Task<PagedTransactions> GetByEmployeeIdAsync(int employeeId, int skip, int take)
        {
            var query = _context.Transactions
                .Include(t => t.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(t => t.SubstituteEmployee)
                .Include(t => t.TransactionType)
                .Include(t => t.Status)
                .Where(t => t.EmployeeId == employeeId)
                .OrderByDescending(t => t.CreationDate);
            var totalCount = await query.CountAsync();
            var items = await query.Skip(skip).Take(take).ToListAsync();
            return new PagedTransactions(items, totalCount);
        }

        public async Task<List<Transaction>> GetByStatusAsync(int statusId)
        {
            return await _context.Transactions
                .Include(t => t.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(t => t.SubstituteEmployee)
                .Include(t => t.TransactionType)
                .Include(t => t.Status)
                .Where(t => t.StatusID == statusId)
                .OrderByDescending(t => t.CreationDate)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetByDepartmentAsync(int departmentId)
        {
            return await _context.Transactions
                .Include(t => t.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(t => t.SubstituteEmployee)
                .Include(t => t.TransactionType)
                .Include(t => t.Status)
                .Where(t => t.Employee!.DepartmentID == departmentId)
                .OrderByDescending(t => t.CreationDate)
                .ToListAsync();
        }

        public async Task<PagedTransactions> GetByManagerAsync(int managerId, int skip, int take)
        {
            var query = _context.Transactions
                .Include(t => t.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(t => t.SubstituteEmployee)
                .Include(t => t.TransactionType)
                .Include(t => t.Status)
                .Where(t => t.Employee!.ManagerId == managerId)
                .OrderByDescending(t => t.CreationDate);
            var totalCount = await query.CountAsync();
            var items = await query.Skip(skip).Take(take).ToListAsync();
            return new PagedTransactions(items, totalCount);
        }

        public async Task<PagedTransactions> GetFilteredAsync(
            int? statusId, 
            int? departmentId, 
            int? employeeId, 
            DateTime? startDate, 
            DateTime? endDate,
            int requestingEmployeeId,
            string role, int skip, int take)
        {
            var query = _context.Transactions
                .Include(t => t.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(t => t.SubstituteEmployee)
                .Include(t => t.TransactionType)
                .Include(t => t.Status)
                .AsQueryable();

            query = role switch
            {
                "Employee" => query.Where(t => t.EmployeeId == requestingEmployeeId),
                "Manager" => query.Where(t => t.EmployeeId == requestingEmployeeId ||
                    t.Employee!.ManagerId == requestingEmployeeId),
                "HR" or "Board" => query,
                _ => throw new UnauthorizedAccessException("Unknown role")
            };

            if (statusId.HasValue)
                query = query.Where(t => t.StatusID == statusId.Value);

            if (departmentId.HasValue)
                query = query.Where(t => t.Employee!.DepartmentID == departmentId.Value);

            if (employeeId.HasValue)
                query = query.Where(t => t.EmployeeId == employeeId.Value);

            if (startDate.HasValue)
                query = query.Where(t => t.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.EndDate <= endDate.Value);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.CreationDate)
                .Skip(skip).Take(take)
                .ToListAsync();
            return new PagedTransactions(items, totalCount);
        }

        public async Task<Transaction> CreateAsync(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<Transaction> UpdateAsync(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
                return false;

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetNextIdAsync()
        {
            return await SequenceIdAllocator.NextTransactionIdAsync(_context);
        }
    }
}
