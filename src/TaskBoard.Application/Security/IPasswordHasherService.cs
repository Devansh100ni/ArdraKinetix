using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Security;

public interface IPasswordHasherService
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string hashedPassword, string providedPassword);
}
