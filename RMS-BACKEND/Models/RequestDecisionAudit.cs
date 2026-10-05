using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMS_BACKEND.Models;

[Table("RequestDecisionAudit")]
public sealed class RequestDecisionAudit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }
    public int TransactionId { get; set; }
    public int ActorEmployeeId { get; set; }
    [MaxLength(16)]
    public string Action { get; set; } = string.Empty;
    public int FromStatusId { get; set; }
    public int ToStatusId { get; set; }
    public DateTime OccurredUtc { get; set; }
    [MaxLength(1000)]
    public string? ResponseMessage { get; set; }
}
