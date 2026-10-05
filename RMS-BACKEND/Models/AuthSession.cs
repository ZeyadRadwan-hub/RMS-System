using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMS_BACKEND.Models;

[Table("AuthSessions")]
public class AuthSession
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public byte[] TokenHash { get; set; } = Array.Empty<byte>();
    public int EmployeeId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
    public Employee Employee { get; set; } = null!;
}
