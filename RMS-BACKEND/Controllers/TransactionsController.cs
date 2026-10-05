using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RMS_BACKEND.DTOs;
using RMS_BACKEND.Services;
using RMS_BACKEND.Security;
using RMS_BACKEND.Data;
using Microsoft.EntityFrameworkCore;

namespace RMS_BACKEND.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ApplicationDbContext _db;

        public TransactionsController(ITransactionService transactionService, ApplicationDbContext db)
        {
            _transactionService = transactionService;
            _db = db;
        }

        /// <summary>
        /// Create a new leave request
        /// </summary>
        [HttpPost]
        [Consumes("application/json")]
        public async Task<ActionResult<TransactionResponseDto>> CreateTransaction(
            [FromBody] CreateTransactionRequestDto request)
        {
            if (User.IsInRole("Board")) return Forbid();
            try
            {
                var result = await _transactionService.CreateTransactionAsync(User.EmployeeId(), request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(52428800)]
        public async Task<ActionResult<TransactionResponseDto>> CreateTransactionWithDocuments(
            [FromForm] CreateTransactionFormDto request, CancellationToken cancellationToken)
        {
            if (User.IsInRole("Board")) return Forbid();
            try
            {
                var documents = request.MedicalDocuments.Count == 0
                    ? Array.Empty<MedicalDocumentInput>()
                    : await MedicalFileValidator.ReadAsync(request.MedicalDocuments, cancellationToken);
                return Ok(await _transactionService.CreateTransactionAsync(User.EmployeeId(), request, documents));
            }
            catch (Exception ex) { return ApiError.Map(this, ex); }
        }

        /// <summary>
        /// Update a pending leave request (only while Pending)
        /// </summary>
        [HttpPut("{id}")]
        [Consumes("application/json")]
        public async Task<ActionResult<TransactionResponseDto>> UpdateTransaction(
            int id,
            [FromBody] UpdateTransactionRequestDto request)
        {
            try
            {
                request.Id = id;
                var result = await _transactionService.UpdateTransactionAsync(User.EmployeeId(), request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(52428800)]
        public async Task<ActionResult<TransactionResponseDto>> UpdateTransactionWithDocuments(
            int id, [FromForm] UpdateTransactionFormDto request, CancellationToken cancellationToken)
        {
            try
            {
                request.Id = id;
                var documents = request.MedicalDocuments.Count == 0
                    ? Array.Empty<MedicalDocumentInput>()
                    : await MedicalFileValidator.ReadAsync(request.MedicalDocuments, cancellationToken);
                return Ok(await _transactionService.UpdateTransactionAsync(User.EmployeeId(), request, documents));
            }
            catch (Exception ex) { return ApiError.Map(this, ex); }
        }

        [HttpGet("{id}/medical-documents")]
        public async Task<IActionResult> GetMedicalDocuments(int id)
        {
            try { await _transactionService.GetTransactionByIdAsync(id, User.EmployeeId()); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex) { return ApiError.Map(this, ex); }
            var documents = await _db.MedicalDocuments.AsNoTracking()
                .Where(d => d.TransactionId == id)
                .OrderBy(d => d.UploadedUtc)
                .Select(d => new { d.Id, d.OriginalName, d.MimeType, d.SizeBytes, d.UploadedUtc })
                .ToListAsync();
            return Ok(documents);
        }

        [HttpGet("{id}/medical-documents/{documentId:guid}")]
        public async Task<IActionResult> DownloadMedicalDocument(int id, Guid documentId)
        {
            try { await _transactionService.GetTransactionByIdAsync(id, User.EmployeeId()); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex) { return ApiError.Map(this, ex); }
            var document = await _db.MedicalDocuments.AsNoTracking()
                .SingleOrDefaultAsync(d => d.TransactionId == id && d.Id == documentId);
            if (document is null) return NotFound();
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(document.Content, document.MimeType, document.OriginalName);
        }

        /// <summary>
        /// Cancel a leave request (before final decision)
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<ActionResult<TransactionResponseDto>> CancelTransaction(
            int id)
        {
            try
            {
                var result = await _transactionService.CancelTransactionAsync(User.EmployeeId(), id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Approve a leave request (Manager/HR/Board)
        /// </summary>
        [HttpPost("{id}/approve")]
        public async Task<ActionResult<TransactionResponseDto>> ApproveTransaction(
            int id,
            [FromBody] ApproveRejectRequestDto request)
        {
            try
            {
                var result = await _transactionService.ApproveTransactionAsync(User.EmployeeId(), id, request.ResponseMessage);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Reject a leave request (Manager/HR/Board)
        /// </summary>
        [HttpPost("{id}/reject")]
        public async Task<ActionResult<TransactionResponseDto>> RejectTransaction(
            int id,
            [FromBody] ApproveRejectRequestDto request)
        {
            try
            {
                var result = await _transactionService.RejectTransactionAsync(User.EmployeeId(), id, request.ResponseMessage);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Get a specific transaction by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionResponseDto>> GetTransaction(
            int id)
        {
            try
            {
                var result = await _transactionService.GetTransactionByIdAsync(id, User.EmployeeId());
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Get my own requests (Employee)
        /// </summary>
        [HttpGet("my-requests")]
        public async Task<ActionResult<PagedResultDto<TransactionResponseDto>>> GetMyRequests(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var result = await _transactionService.GetMyRequestsAsync(User.EmployeeId(), page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Get my team's requests (Manager)
        /// </summary>
        [HttpGet("my-team-requests")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<PagedResultDto<TransactionResponseDto>>> GetMyTeamRequests(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var result = await _transactionService.GetMyTeamRequestsAsync(User.EmployeeId(), page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Get all requests (HR/Board)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "HR,Board")]
        public async Task<ActionResult<PagedResultDto<TransactionResponseDto>>> GetAllRequests(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var result = await _transactionService.GetAllRequestsAsync(User.EmployeeId(), User.RmsRole(), page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }

        /// <summary>
        /// Get filtered requests with filters
        /// </summary>
        [HttpPost("filter")]
        public async Task<ActionResult<PagedResultDto<TransactionResponseDto>>> GetFilteredRequests(
            [FromBody] DashboardFilterDto filter,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var result = await _transactionService.GetFilteredRequestsAsync(
                    filter, User.EmployeeId(), User.RmsRole(), page, pageSize);
                return Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                return ApiError.Map(this, ex);
            }
        }
    }
}
