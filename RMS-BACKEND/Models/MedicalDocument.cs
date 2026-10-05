using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMS_BACKEND.Models;

[Table("MedicalDocuments")]
public class MedicalDocument
{
    [Key]
    public Guid Id { get; set; }
    public int TransactionId { get; set; }
    public int UploadedByEmployeeId { get; set; }
    [MaxLength(255)]
    public string OriginalName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;
    public int SizeBytes { get; set; }
    public byte[] Sha256 { get; set; } = Array.Empty<byte>();
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public DateTime UploadedUtc { get; set; }
    public Transaction Transaction { get; set; } = null!;
    public Employee UploadedByEmployee { get; set; } = null!;
}
