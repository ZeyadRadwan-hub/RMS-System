using RMS_BACKEND.DTOs;
using RMS_BACKEND.Models;
using RMS_BACKEND.Repositories;
using RMS_BACKEND.Data;
using Microsoft.EntityFrameworkCore;

namespace RMS_BACKEND.Services
{
    public interface ITransactionService
    {
        Task<TransactionResponseDto> CreateTransactionAsync(int employeeId, CreateTransactionRequestDto request, IReadOnlyList<MedicalDocumentInput>? documents = null);
        Task<TransactionResponseDto> UpdateTransactionAsync(int employeeId, UpdateTransactionRequestDto request, IReadOnlyList<MedicalDocumentInput>? documents = null);
        Task<TransactionResponseDto> CancelTransactionAsync(int employeeId, int transactionId);
        Task<TransactionResponseDto> ApproveTransactionAsync(int approverId, int transactionId, string responseMessage);
        Task<TransactionResponseDto> RejectTransactionAsync(int rejecterId, int transactionId, string responseMessage);
        Task<TransactionResponseDto> GetTransactionByIdAsync(int transactionId, int requestingEmployeeId);
        Task<PagedResultDto<TransactionResponseDto>> GetMyRequestsAsync(int employeeId, int page, int pageSize);
        Task<PagedResultDto<TransactionResponseDto>> GetMyTeamRequestsAsync(int managerId, int page, int pageSize);
        Task<PagedResultDto<TransactionResponseDto>> GetAllRequestsAsync(int requestingEmployeeId, string role, int page, int pageSize);
        Task<PagedResultDto<TransactionResponseDto>> GetFilteredRequestsAsync(DashboardFilterDto filter, int requestingEmployeeId, string role, int page, int pageSize);
    }

    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IRequestStateMachineService _stateMachine;
        private readonly ILeaveCalculationService _calculationService;
        private readonly TransactionRequestValidator _validator;
        private readonly ApplicationDbContext _db;

        public TransactionService(
            ITransactionRepository transactionRepo,
            IEmployeeRepository employeeRepo,
            IRequestStateMachineService stateMachine,
            ILeaveCalculationService calculationService,
            TransactionRequestValidator validator,
            ApplicationDbContext db)
        {
            _transactionRepo = transactionRepo;
            _employeeRepo = employeeRepo;
            _stateMachine = stateMachine;
            _calculationService = calculationService;
            _validator = validator;
            _db = db;
        }

        public async Task<TransactionResponseDto> CreateTransactionAsync(int employeeId, CreateTransactionRequestDto request, IReadOnlyList<MedicalDocumentInput>? documents = null)
        {
            ValidateMedicalDocuments(request.TransactionTypesID, documents, false);
            var employee = await _employeeRepo.GetByIdAsync(employeeId);
            if (employee == null)
                throw new KeyNotFoundException("Employee not found");

            await using var dbTransaction = await _db.Database.BeginTransactionAsync();
            await LockEmployeeRequestsAsync(employeeId);
            await _validator.ValidateAsync(employee, request.TransactionTypesID, request.StartDate,
                request.EndDate, request.SubstituteEmployeeId, request.LeaveRationale);

            var nextId = await _transactionRepo.GetNextIdAsync();

            var transaction = new Transaction
            {
                Id = nextId,
                EmployeeId = employeeId,
                TransactionTypesID = request.TransactionTypesID,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                SubstituteEmployeeId = request.SubstituteEmployeeId,
                LeaveRationale = request.LeaveRationale,
                StatusID = TransactionStatus.Pending,
                CreationDate = DateTime.Now,
                ResponseMessage = string.Empty
            };

            var created = await _transactionRepo.CreateAsync(transaction);
            await SaveDocumentsAsync(created.Id, employeeId, documents);
            await dbTransaction.CommitAsync();
            var result = await _transactionRepo.GetByIdAsync(created.Id);

            return MapToDto(result!, employeeId);
        }

        private async Task LockEmployeeRequestsAsync(int employeeId)
        {
            var resource = $"RMS:EmployeeRequests:{employeeId}";
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"DECLARE @lockResult int; EXEC @lockResult = sys.sp_getapplock @Resource={resource}, @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=60000; IF @lockResult < 0 THROW 51080, 'Could not acquire request lock.', 1;");
        }

        private async Task LockRequestAsync(int transactionId)
        {
            var resource = $"RMS:RequestDecision:{transactionId}";
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"DECLARE @lockResult int; EXEC @lockResult = sys.sp_getapplock @Resource={resource}, @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=60000; IF @lockResult < 0 THROW 51081, 'Could not acquire request decision lock.', 1;");
        }

        public async Task<TransactionResponseDto> UpdateTransactionAsync(int employeeId, UpdateTransactionRequestDto request, IReadOnlyList<MedicalDocumentInput>? documents = null)
        {
            await using var dbTransaction = await _db.Database.BeginTransactionAsync();
            await LockRequestAsync(request.Id);
            var transaction = await _transactionRepo.GetByIdAsync(request.Id);
            if (transaction == null)
                throw new KeyNotFoundException("Transaction not found");

            // Check if employee can edit
            if (!_stateMachine.CanEdit(transaction.StatusID, employeeId, transaction.EmployeeId))
                throw new ArgumentException("Cannot edit this request. Only pending requests can be edited.");

            var hasExistingDocuments = await _db.MedicalDocuments.AnyAsync(d => d.TransactionId == transaction.Id);
            ValidateMedicalDocuments(request.TransactionTypesID, documents, hasExistingDocuments);
            if (documents is { Count: > 0 } &&
                await _db.MedicalDocuments.CountAsync(d => d.TransactionId == transaction.Id) + documents.Count > MedicalFileValidator.MaxFiles)
                throw new ArgumentException("At most five medical documents are permitted per request");
            if (request.TransactionTypesID != 1 && hasExistingDocuments)
                throw new ArgumentException("A request with medical documents cannot change away from sick leave");

            var employee = await _employeeRepo.GetByIdAsync(employeeId)
                ?? throw new KeyNotFoundException("Employee not found");
            await _validator.ValidateAsync(employee, request.TransactionTypesID, request.StartDate,
                request.EndDate, request.SubstituteEmployeeId, request.LeaveRationale, transaction.Id);

            // Update fields
            transaction.TransactionTypesID = request.TransactionTypesID;
            transaction.StartDate = request.StartDate;
            transaction.EndDate = request.EndDate;
            transaction.SubstituteEmployeeId = request.SubstituteEmployeeId;
            transaction.LeaveRationale = request.LeaveRationale;

            var updated = await _transactionRepo.UpdateAsync(transaction);
            await SaveDocumentsAsync(updated.Id, employeeId, documents);
            await dbTransaction.CommitAsync();
            var result = await _transactionRepo.GetByIdAsync(updated.Id);

            return MapToDto(result!, employeeId);
        }

        private static void ValidateMedicalDocuments(int typeId, IReadOnlyList<MedicalDocumentInput>? documents, bool hasExisting)
        {
            if (typeId == 1 && !hasExisting && (documents is null || documents.Count == 0))
                throw new ArgumentException("Sick leave requires a medical document");
            if (typeId != 1 && documents is { Count: > 0 })
                throw new ArgumentException("Medical documents are only permitted for sick leave");
        }

        private async Task SaveDocumentsAsync(int transactionId, int employeeId, IReadOnlyList<MedicalDocumentInput>? documents)
        {
            if (documents is not { Count: > 0 }) return;
            foreach (var document in documents)
                _db.MedicalDocuments.Add(new MedicalDocument
                {
                    Id = Guid.NewGuid(), TransactionId = transactionId, UploadedByEmployeeId = employeeId,
                    OriginalName = document.OriginalName, MimeType = document.MimeType,
                    SizeBytes = document.Content.Length, Sha256 = document.Sha256,
                    Content = document.Content, UploadedUtc = DateTime.UtcNow
                });
            await _db.SaveChangesAsync();
        }

        public async Task<TransactionResponseDto> CancelTransactionAsync(int employeeId, int transactionId)
        {
            await using var decisionTransaction = await _db.Database.BeginTransactionAsync();
            await LockRequestAsync(transactionId);
            var transaction = await _transactionRepo.GetByIdAsync(transactionId);
            if (transaction == null)
                throw new KeyNotFoundException("Transaction not found");

            // Check if employee can cancel
            if (!_stateMachine.CanCancel(transaction.StatusID, employeeId, transaction.EmployeeId))
                throw new ArgumentException("Cannot cancel this request. Request is already finalized.");

            transaction.StatusID = TransactionStatus.CancelledByEmployee;
            transaction.ResponseDate = DateTime.Now;
            transaction.ResponseMessage = "Cancelled by employee";

            var updated = await _transactionRepo.UpdateAsync(transaction);
            await decisionTransaction.CommitAsync();
            var result = await _transactionRepo.GetByIdAsync(updated.Id);

            return MapToDto(result!, employeeId);
        }

        public async Task<TransactionResponseDto> ApproveTransactionAsync(int approverId, int transactionId, string responseMessage)
        {
            if (responseMessage?.Length > 1000)
                throw new ArgumentException("Response message exceeds 1000 characters");
            await using var decisionTransaction = await _db.Database.BeginTransactionAsync();
            await LockRequestAsync(transactionId);
            var transaction = await _transactionRepo.GetByIdAsync(transactionId);
            if (transaction == null)
                throw new KeyNotFoundException("Transaction not found");

            var approver = await _employeeRepo.GetByIdAsync(approverId);
            if (approver == null)
                throw new KeyNotFoundException("Approver not found");

            var role = _stateMachine.DetermineUserRole(approver.DepartmentID, approver.EmployeeRole == EmployeeRole.Manager);

            if (role == "Manager" && transaction.Employee?.ManagerId != approverId)
                throw new UnauthorizedAccessException("Request is outside this manager's team");

            // Check if approver can approve
            if (!_stateMachine.CanApprove(transaction.StatusID, role, approver.DepartmentID, transaction.Employee?.ManagerId, approverId))
                throw new UnauthorizedAccessException("You do not have permission to approve this request.");

            // Update status
            var previousStatus = transaction.StatusID;
            var nextStatus = _stateMachine.GetNextStatusOnApproval(transaction.StatusID, role);
            transaction.StatusID = nextStatus;
            transaction.ResponseDate = DateTime.Now;
            transaction.ResponseMessage = responseMessage;

            var updated = await _transactionRepo.UpdateAsync(transaction);
            _db.RequestDecisionAudit.Add(new RequestDecisionAudit
            {
                Id = Guid.NewGuid(), TransactionId = transactionId, ActorEmployeeId = approverId,
                Action = "Approve", FromStatusId = previousStatus, ToStatusId = nextStatus,
                OccurredUtc = DateTime.UtcNow, ResponseMessage = responseMessage
            });
            await _db.SaveChangesAsync();
            await decisionTransaction.CommitAsync();
            var result = await _transactionRepo.GetByIdAsync(updated.Id);

            return MapToDto(result!, approverId);
        }

        public async Task<TransactionResponseDto> RejectTransactionAsync(int rejecterId, int transactionId, string responseMessage)
        {
            if (responseMessage?.Length > 1000)
                throw new ArgumentException("Response message exceeds 1000 characters");
            await using var decisionTransaction = await _db.Database.BeginTransactionAsync();
            await LockRequestAsync(transactionId);
            var transaction = await _transactionRepo.GetByIdAsync(transactionId);
            if (transaction == null)
                throw new KeyNotFoundException("Transaction not found");

            var rejecter = await _employeeRepo.GetByIdAsync(rejecterId);
            if (rejecter == null)
                throw new KeyNotFoundException("Rejecter not found");

            var role = _stateMachine.DetermineUserRole(rejecter.DepartmentID, rejecter.EmployeeRole == EmployeeRole.Manager);

            if (role == "Manager" && transaction.Employee?.ManagerId != rejecterId)
                throw new UnauthorizedAccessException("Request is outside this manager's team");

            // Check if rejecter can reject
            if (!_stateMachine.CanReject(transaction.StatusID, role, rejecter.DepartmentID, transaction.Employee?.ManagerId, rejecterId))
                throw new UnauthorizedAccessException("You do not have permission to reject this request.");

            // Update status
            var previousStatus = transaction.StatusID;
            var nextStatus = _stateMachine.GetNextStatusOnRejection(transaction.StatusID, role);
            transaction.StatusID = nextStatus;
            transaction.ResponseDate = DateTime.Now;
            transaction.ResponseMessage = responseMessage;

            var updated = await _transactionRepo.UpdateAsync(transaction);
            _db.RequestDecisionAudit.Add(new RequestDecisionAudit
            {
                Id = Guid.NewGuid(), TransactionId = transactionId, ActorEmployeeId = rejecterId,
                Action = "Reject", FromStatusId = previousStatus, ToStatusId = nextStatus,
                OccurredUtc = DateTime.UtcNow, ResponseMessage = responseMessage
            });
            await _db.SaveChangesAsync();
            await decisionTransaction.CommitAsync();
            var result = await _transactionRepo.GetByIdAsync(updated.Id);

            return MapToDto(result!, rejecterId);
        }

        public async Task<TransactionResponseDto> GetTransactionByIdAsync(int transactionId, int requestingEmployeeId)
        {
            var transaction = await _transactionRepo.GetByIdAsync(transactionId);
            if (transaction == null)
                throw new KeyNotFoundException("Transaction not found");

            var requester = await _employeeRepo.GetByIdAsync(requestingEmployeeId);
            if (requester is null) throw new UnauthorizedAccessException("Unknown requester");
            var role = _stateMachine.DetermineUserRole(requester.DepartmentID,
                requester.EmployeeRole == EmployeeRole.Manager);
            if (role is not ("HR" or "Board") && transaction.EmployeeId != requestingEmployeeId &&
                !(role == "Manager" && transaction.Employee?.ManagerId == requestingEmployeeId))
                throw new UnauthorizedAccessException("Request is outside your scope");

            return MapToDto(transaction, requestingEmployeeId);
        }

        public async Task<PagedResultDto<TransactionResponseDto>> GetMyRequestsAsync(int employeeId, int page, int pageSize)
        {
            var pageSpec = NormalizePage(page, pageSize);
            var transactions = await _transactionRepo.GetByEmployeeIdAsync(employeeId, pageSpec.Skip, pageSpec.Take);
            return ToPage(transactions, employeeId, pageSpec.Page, pageSpec.Take);
        }

        public async Task<PagedResultDto<TransactionResponseDto>> GetMyTeamRequestsAsync(int managerId, int page, int pageSize)
        {
            var pageSpec = NormalizePage(page, pageSize);
            var transactions = await _transactionRepo.GetByManagerAsync(managerId, pageSpec.Skip, pageSpec.Take);
            return ToPage(transactions, managerId, pageSpec.Page, pageSpec.Take);
        }

        public async Task<PagedResultDto<TransactionResponseDto>> GetAllRequestsAsync(int requestingEmployeeId, string role, int page, int pageSize)
        {
            if (role is not ("HR" or "Board"))
                throw new UnauthorizedAccessException("Organization-wide requests require HR or Board");
            var pageSpec = NormalizePage(page, pageSize);
            var transactions = await _transactionRepo.GetAllAsync(pageSpec.Skip, pageSpec.Take);
            return ToPage(transactions, requestingEmployeeId, pageSpec.Page, pageSpec.Take);
        }

        public async Task<PagedResultDto<TransactionResponseDto>> GetFilteredRequestsAsync(DashboardFilterDto filter, int requestingEmployeeId, string role, int page, int pageSize)
        {
            var pageSpec = NormalizePage(page, pageSize);
            var transactions = await _transactionRepo.GetFilteredAsync(
                filter.StatusID,
                filter.DepartmentID,
                filter.EmployeeId,
                filter.StartDate,
                filter.EndDate,
                requestingEmployeeId,
                role,
                pageSpec.Skip,
                pageSpec.Take);

            return ToPage(transactions, requestingEmployeeId, pageSpec.Page, pageSpec.Take);
        }

        private static (int Page, int Take, int Skip) NormalizePage(int page, int pageSize)
        {
            var normalizedPage = Math.Max(1, page);
            var normalizedTake = Math.Clamp(pageSize <= 0 ? 100 : pageSize, 1, 200);
            var skipLong = ((long)normalizedPage - 1) * normalizedTake;
            return (normalizedPage, normalizedTake, skipLong > int.MaxValue ? int.MaxValue : (int)skipLong);
        }

        private PagedResultDto<TransactionResponseDto> ToPage(
            PagedTransactions transactions, int requestingEmployeeId, int page, int pageSize)
        {
            return new PagedResultDto<TransactionResponseDto>
            {
                Items = transactions.Items.Select(t => MapToDto(t, requestingEmployeeId)).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = transactions.TotalCount
            };
        }

        private TransactionResponseDto MapToDto(Transaction transaction, int requestingEmployeeId)
        {
            var calculatedDays = transaction.TransactionType != null 
                ? Math.Abs(_calculationService.CalculateLeaveDays(transaction, transaction.TransactionType))
                : 0;

            var canEdit = _stateMachine.CanEdit(transaction.StatusID, requestingEmployeeId, transaction.EmployeeId);
            var canCancel = _stateMachine.CanCancel(transaction.StatusID, requestingEmployeeId, transaction.EmployeeId);

            return new TransactionResponseDto
            {
                Id = transaction.Id,
                TransactionTypesID = transaction.TransactionTypesID,
                TransactionTypeName = transaction.TransactionType?.Name ?? "",
                StartDate = transaction.StartDate,
                EndDate = transaction.EndDate,
                SubstituteEmployeeId = transaction.SubstituteEmployeeId,
                SubstituteEmployeeName = transaction.SubstituteEmployee?.Name ?? "",
                LeaveRationale = transaction.LeaveRationale,
                ResponseDate = transaction.ResponseDate,
                ResponseMessage = transaction.ResponseMessage,
                StatusID = transaction.StatusID,
                StatusName = transaction.Status?.StatusName ?? "",
                CreationDate = transaction.CreationDate,
                EmployeeId = transaction.EmployeeId,
                EmployeeName = transaction.Employee?.Name ?? "",
                EmployeeCode = transaction.Employee?.Code ?? "",
                DepartmentName = transaction.Employee?.Department?.StatusName ?? "",
                CalculatedDays = calculatedDays,
                CanEdit = canEdit,
                CanCancel = canCancel
            };
        }
    }
}
