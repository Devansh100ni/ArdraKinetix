using TaskBoard.Application.DTOs.Auth;

namespace TaskBoard.Application.Features.Authentication;

public interface IAuthenticationService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
