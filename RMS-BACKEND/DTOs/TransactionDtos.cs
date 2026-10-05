using System.ComponentModel.DataAnnotations;

namespace RMS_BACKEND.DTOs
{
    public class CreateTransactionRequestDto : IValidatableObject
    {
        [Range(1, int.MaxValue)]
        public int TransactionTypesID { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Range(1, int.MaxValue)]
        public int? SubstituteEmployeeId { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(500, MinimumLength = 1)]
        public string? LeaveRationale { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            TransactionRequestValidation.Validate(StartDate, EndDate);
    }

    public class CreateTransactionFormDto : CreateTransactionRequestDto
    {
        public List<IFormFile> MedicalDocuments { get; set; } = new();
    }

    public class UpdateTransactionRequestDto : IValidatableObject
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int TransactionTypesID { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Range(1, int.MaxValue)]
        public int? SubstituteEmployeeId { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(500, MinimumLength = 1)]
        public string? LeaveRationale { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
            TransactionRequestValidation.Validate(StartDate, EndDate);
    }

    public class UpdateTransactionFormDto : UpdateTransactionRequestDto
    {
        public List<IFormFile> MedicalDocuments { get; set; } = new();
    }

    public class TransactionResponseDto
    {
        public int Id { get; set; }
        public int TransactionTypesID { get; set; }
        public string TransactionTypeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? SubstituteEmployeeId { get; set; }
        public string SubstituteEmployeeName { get; set; } = string.Empty;
        public string? LeaveRationale { get; set; }
        public DateTime? ResponseDate { get; set; }
        public string? ResponseMessage { get; set; }
        public int StatusID { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public double CalculatedDays { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCancel { get; set; }
    }

    public sealed class PagedResultDto<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public bool HasNext => Page * PageSize < TotalCount;
    }

    public class ApproveRejectRequestDto
    {
        public int TransactionId { get; set; }

        [StringLength(1000)]
        public string ResponseMessage { get; set; } = string.Empty;
    }

    public class CancelRequestDto
    {
        [Range(1, int.MaxValue)]
        public int TransactionId { get; set; }
    }

    internal static class TransactionRequestValidation
    {
        public static IEnumerable<ValidationResult> Validate(DateTime start, DateTime end)
        {
            if (start == default)
                yield return new ValidationResult("Start date is required.", new[] { nameof(CreateTransactionRequestDto.StartDate) });
            if (end == default)
                yield return new ValidationResult("End date is required.", new[] { nameof(CreateTransactionRequestDto.EndDate) });

            if (start != default && end != default)
            {
                if (start.Year is < 2000 or > 2100)
                    yield return new ValidationResult("Start date is outside the supported range.", new[] { nameof(CreateTransactionRequestDto.StartDate) });
                if (end.Year is < 2000 or > 2100)
                    yield return new ValidationResult("End date is outside the supported range.", new[] { nameof(CreateTransactionRequestDto.EndDate) });
                if (end.Date < start.Date)
                    yield return new ValidationResult("End date must be on or after start date.", new[] { nameof(CreateTransactionRequestDto.EndDate) });
            }
        }
    }
}
