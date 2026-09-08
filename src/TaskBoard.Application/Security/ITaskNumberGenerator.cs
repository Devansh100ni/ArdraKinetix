namespace TaskBoard.Application.Security;

public interface ITaskNumberGenerator
{
    Task<string> GenerateNextTaskNumberAsync(Guid tenantId, string prefix, CancellationToken cancellationToken = default);
}
