namespace TaskBoard.Application.Security;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Email { get; }
    string? FullName { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
    bool IsInRole(string role);
}
