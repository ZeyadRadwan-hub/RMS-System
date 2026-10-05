using System.IO.Compression;
using System.Security.Cryptography;

namespace RMS_BACKEND.Services;

public sealed record MedicalDocumentInput(string OriginalName, string MimeType, byte[] Content, byte[] Sha256);

public static class MedicalFileValidator
{
    public const int MaxFiles = 5;
    public const int MaxFileBytes = 10 * 1024 * 1024;

    public static async Task<IReadOnlyList<MedicalDocumentInput>> ReadAsync(
        IReadOnlyList<IFormFile> files, CancellationToken cancellationToken)
    {
        if (files.Count is < 1 or > MaxFiles)
            throw new ArgumentException("Sick leave requires 1–5 medical documents");

        var documents = new List<MedicalDocumentInput>(files.Count);
        foreach (var file in files)
        {
            if (file.Length is < 1 or > MaxFileBytes)
                throw new ArgumentException("Each medical document must be 1 byte to 10 MB");
            var name = file.FileName.Replace('\\', '/').Split('/').Last();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 255 || name.Any(char.IsControl))
                throw new ArgumentException("Invalid medical document filename");

            await using var input = file.OpenReadStream();
            await using var buffer = new MemoryStream((int)file.Length);
            await input.CopyToAsync(buffer, cancellationToken);
            if (buffer.Length != file.Length || buffer.Length > MaxFileBytes)
                throw new ArgumentException("Medical document size changed during upload");
            var bytes = buffer.ToArray();
            var mime = DetectMime(name, bytes);
            if (mime is null || (!string.IsNullOrEmpty(file.ContentType) &&
                !string.Equals(file.ContentType, mime, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Unsupported or mismatched medical document type");
            documents.Add(new MedicalDocumentInput(name, mime, bytes, SHA256.HashData(bytes)));
        }
        return documents;
    }

    private static string? DetectMime(string name, byte[] bytes)
    {
        var ext = Path.GetExtension(name).ToLowerInvariant();
        if (ext == ".pdf" && bytes.AsSpan().StartsWith("%PDF-"u8))
            return "application/pdf";
        if ((ext is ".jpg" or ".jpeg") && bytes.Length >= 3 &&
            bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return "image/jpeg";
        if (ext == ".png" && bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return "image/png";
        if (ext == ".doc" && bytes.AsSpan().StartsWith(new byte[] { 208, 207, 17, 224, 161, 177, 26, 225 }))
            return "application/msword";
        if (ext == ".docx" && bytes.AsSpan().StartsWith(new byte[] { 80, 75, 3, 4 }))
        {
            try
            {
                using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
                if (archive.GetEntry("[Content_Types].xml") is not null &&
                    archive.GetEntry("word/document.xml") is not null)
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            }
            catch (InvalidDataException) { }
        }
        return null;
    }
}
