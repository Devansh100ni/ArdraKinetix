using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Scrum;

namespace TaskBoard.Application.Features.ScrumBoards;

public interface IScrumBoardService
{
    Task<ScrumBoardDto?> GetActiveBoardAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScrumBoardDto>> GetAllBoardsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<Result<ScrumBoardDto>> CreateBoardAsync(CreateScrumBoardDto dto, CancellationToken cancellationToken = default);
    Task<Result<ScrumBoardColumnDto>> AddColumnAsync(CreateScrumColumnDto dto, Guid boardId, CancellationToken cancellationToken = default);
    Task<Result<ScrumBoardColumnDto>> UpdateColumnAsync(UpdateScrumColumnDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteColumnAsync(Guid columnId, CancellationToken cancellationToken = default);
}
