namespace TaskBoard.Application.DTOs.Attachments;

public class TaskAttachmentDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FormattedSize => FileSize < 1024 
        ? $"{FileSize} B" 
        : FileSize < 1024 * 1024 
            ? $"{FileSize / 1024.0:F1} KB" 
            : $"{FileSize / (1024.0 * 1024.0):F1} MB";
    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    public bool IsPdf => ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    public Guid UploadedBy { get; set; }
    public string UploaderName { get; set; } = string.Empty;
    public DateTime UploadedOn { get; set; }
    public string DownloadUrl => $"/Tasks/DownloadAttachment/{Id}";
    public string PreviewUrl => $"/Tasks/PreviewAttachment/{Id}";
}

public class UploadAttachmentDto
{
    public Guid TaskId { get; set; }
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}
