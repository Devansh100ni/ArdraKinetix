namespace TaskBoard.Domain.Enums;

public enum AuditActionType
{
    Created = 1,
    Updated = 2,
    StatusChanged = 3,
    PriorityChanged = 4,
    Assigned = 5,
    Unassigned = 6,
    CommentAdded = 7,
    CommentDeleted = 8,
    AttachmentAdded = 9,
    AttachmentDeleted = 10,
    ChildTaskCreated = 11,
    MovedOnBoard = 12,
    SoftDeleted = 13,
    Restored = 14
}
