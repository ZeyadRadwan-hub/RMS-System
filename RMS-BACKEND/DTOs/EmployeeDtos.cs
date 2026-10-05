using System.ComponentModel.DataAnnotations;

namespace RMS_BACKEND.DTOs
{
    public class EmployeeListDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime DateOfEmployment { get; set; }
        public string EmployeeRole { get; set; } = string.Empty;
        public string EmployeeLevel { get; set; } = string.Empty;
        public int EmployeeLevelId { get; set; }
        public int AnnualLeaveEntitlement { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsManager { get; set; }
    }

    public class CreateEmployeeDto : IValidatableObject
    {
        [Required(AllowEmptyStrings = false)]
        [StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(128, MinimumLength = 9)]
        public string Password { get; set; } = string.Empty;

        public DateTime DateOfEmployment { get; set; }

        [Range(0, 1)]
        public short EmployeeRole { get; set; }

        [Range(1, int.MaxValue)]
        public int EmployeeLevelId { get; set; }

        [Range(1, int.MaxValue)]
        public int? ManagerId { get; set; }

        [Range(1, int.MaxValue)]
        public int DepartmentID { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateOfEmployment == default)
                yield return new ValidationResult("Date of employment is required.", new[] { nameof(DateOfEmployment) });
        }
    }

    public class UpdateEmployeeDto : IValidatableObject
    {
        public int Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        public DateTime DateOfEmployment { get; set; }

        [Range(0, 1)]
        public short EmployeeRole { get; set; }

        [Range(1, int.MaxValue)]
        public int EmployeeLevelId { get; set; }

        [Range(1, int.MaxValue)]
        public int? ManagerId { get; set; }

        [Range(1, int.MaxValue)]
        public int DepartmentID { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateOfEmployment == default)
                yield return new ValidationResult("Date of employment is required.", new[] { nameof(DateOfEmployment) });
        }
    }

    public class EmployeeFilterDto
    {
        [StringLength(50)]
        public string? Code { get; set; }

        [StringLength(200)]
        public string? Name { get; set; }

        [Range(1, int.MaxValue)]
        public int? DepartmentID { get; set; }

        [Range(1, int.MaxValue)]
        public int? EmployeeLevelId { get; set; }
        public bool? IsActive { get; set; }
    }
}
