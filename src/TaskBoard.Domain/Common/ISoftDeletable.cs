namespace TaskBoard.Domain.Common;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedOn { get; set; }
    Guid? DeletedBy { get; set; }
}
