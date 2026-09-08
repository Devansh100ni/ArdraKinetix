using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class TaskAttachment : BaseEntity, ISoftDeletable
{
    public Guid TaskId { get; set; }
    public virtual TaskItem Task { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;

    public Guid UploadedBy { get; set; }
    public virtual User Uploader { get; set; } = null!;
    public DateTime UploadedOn { get; set; } = DateTime.UtcNow;

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
    public Guid? DeletedBy { get; set; }
}
