using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMS_BACKEND.Models
{
    [Table("EmployeeLevels")]
    public class EmployeeLevel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string LevelName { get; set; } = string.Empty;

        [Required]
        public int AnnualLeaveEntitlement { get; set; }

        // Navigation Properties
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
