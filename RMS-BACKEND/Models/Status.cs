using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMS_BACKEND.Models
{
    [Table("Statuses")]
    public class Status
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string StatusName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string StatusType { get; set; } = string.Empty;

        // Navigation Properties
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
