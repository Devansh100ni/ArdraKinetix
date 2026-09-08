namespace TaskBoard.Domain.Exceptions;

public class TenantAccessDeniedException : Exception
{
    public Guid? RequestedTenantId { get; }
    public Guid? CurrentUserTenantId { get; }

    public TenantAccessDeniedException(string message) : base(message)
    {
    }

    public TenantAccessDeniedException(Guid? requestedTenantId)
        : this(requestedTenantId, null)
    {
    }

    public TenantAccessDeniedException(Guid? requestedTenantId, Guid? currentUserTenantId)
        : base($"Access denied: Cannot access resource belonging to tenant {requestedTenantId} with user context {currentUserTenantId}.")
    {
        RequestedTenantId = requestedTenantId;
        CurrentUserTenantId = currentUserTenantId;
    }
}
