namespace RMS_BACKEND.DTOs
{
    public class CreateTransactionRequestDto
    {
        public int TransactionTypesID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? SubstituteEmployeeId { get; set; }
        public string? LeaveRationale { get; set; }
    }

    public class CreateTransactionFormDto : CreateTransactionRequestDto
    {
        public List<IFormFile> MedicalDocuments { get; set; } = new();
    }

    public class UpdateTransactionRequestDto
    {
        public int Id { get; set; }
        public int TransactionTypesID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? SubstituteEmployeeId { get; set; }
        public string? LeaveRationale { get; set; }
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

    public class ApproveRejectRequestDto
    {
        public int TransactionId { get; set; }
        public string ResponseMessage { get; set; } = string.Empty;
    }

    public class CancelRequestDto
    {
        public int TransactionId { get; set; }
    }
}
