using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Attachments;
using TaskBoard.Application.DTOs.Comments;
using TaskBoard.Application.DTOs.Tasks;

namespace TaskBoard.Application.Features.Tasks;

public interface ITaskService
{
    Task<TaskDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<TaskDto>> GetPagedAsync(TaskFilterCriteria criteria, CancellationToken cancellationToken = default);
    Task<Result<TaskDetailDto>> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken = default);
    Task<Result<TaskDetailDto>> UpdateAsync(UpdateTaskDto dto, CancellationToken cancellationToken = default);
    Task<Result<TaskDto>> MoveTaskAsync(MoveTaskDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // Comments
    Task<Result<TaskCommentDto>> AddCommentAsync(CreateTaskCommentDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default);

    // Attachments
    Task<Result<TaskAttachmentDto>> UploadAttachmentAsync(UploadAttachmentDto dto, CancellationToken cancellationToken = default);
    Task<Result<(Stream stream, string contentType, string fileName)>> GetAttachmentFileAsync(Guid attachmentId, CancellationToken cancellationToken = default);
    Task<Result> DeleteAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default);
}
