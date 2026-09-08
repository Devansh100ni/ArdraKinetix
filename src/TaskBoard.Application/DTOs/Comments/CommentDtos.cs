namespace TaskBoard.Application.DTOs.Comments;

public class TaskCommentDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class CreateTaskCommentDto
{
    public Guid TaskId { get; set; }
    public string Comment { get; set; } = string.Empty;
}
