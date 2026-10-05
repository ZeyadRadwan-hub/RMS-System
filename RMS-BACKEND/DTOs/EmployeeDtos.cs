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

    public class CreateEmployeeDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime DateOfEmployment { get; set; }
        public short EmployeeRole { get; set; }
        public int EmployeeLevelId { get; set; }
        public int? ManagerId { get; set; }
        public int DepartmentID { get; set; }
    }

    public class UpdateEmployeeDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime DateOfEmployment { get; set; }
        public short EmployeeRole { get; set; }
        public int EmployeeLevelId { get; set; }
        public int? ManagerId { get; set; }
        public int DepartmentID { get; set; }
    }

    public class EmployeeFilterDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public int? DepartmentID { get; set; }
        public int? EmployeeLevelId { get; set; }
        public bool? IsActive { get; set; }
    }
}
