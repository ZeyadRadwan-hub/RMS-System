using System.ComponentModel.DataAnnotations;

namespace RMS_BACKEND.DTOs
{
    public class LoginRequestDto
    {
        [Required(AllowEmptyStrings = false)]
        [StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(128)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Board, HR, Manager, Employee
        public bool IsManager { get; set; }
        public int? ManagerId { get; set; }
        public string Token { get; set; } = string.Empty;
    }

    public class ChangePasswordRequestDto
    {
        [Required(AllowEmptyStrings = false)]
        [StringLength(128)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [StringLength(128, MinimumLength = 9)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
