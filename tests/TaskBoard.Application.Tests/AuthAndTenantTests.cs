using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.DTOs.Auth;
using TaskBoard.Application.DTOs.Tenants;
using TaskBoard.Application.Features.Authentication;
using TaskBoard.Application.Features.Tenants;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using Xunit;

namespace TaskBoard.Application.Tests;

public class AuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasherService> _hasherMock = new();
    private readonly Mock<IJwtTokenService> _jwtMock = new();
    private readonly Mock<IAdminAuditRepository> _auditRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task LoginAsync_ValidDeveloperCredentials_ReturnsTokenWithMultiTenants()
    {
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "devuser",
            Email = "dev@example.com",
            PasswordHash = "HASH",
            IsActive = true
        };
        var t1 = new Tenant { Id = tenant1, Code = "ACM", Name = "Acme", IsActive = true };
        var t2 = new Tenant { Id = tenant2, Code = "BET", Name = "Beta", IsActive = true };
        user.UserRoles.Add(new UserRole { Role = new Role { Name = SystemRoles.Developer } });
        user.UserTenants.Add(new UserTenant { TenantId = tenant1, Tenant = t1 });
        user.UserTenants.Add(new UserTenant { TenantId = tenant2, Tenant = t2 });

        _userRepoMock.Setup(r => r.GetByEmailOrUsernameAsync("devuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _hasherMock.Setup(h => h.VerifyPassword(user, "HASH", "ValidPass123!"))
            .Returns(true);

        _jwtMock.Setup(j => j.GenerateToken(user, SystemRoles.Developer, tenant1, It.Is<IReadOnlyList<Guid>>(l => l.Count == 2)))
            .Returns("JWT_DEV_TOKEN");

        var service = new AuthenticationService(
            _userRepoMock.Object,
            _hasherMock.Object,
            _jwtMock.Object,
            _currentUserMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<AuthenticationService>.Instance);

        var response = await service.LoginAsync(new LoginRequest
        {
            Identifier = "devuser",
            Password = "ValidPass123!"
        });

        response.Success.Should().BeTrue();
        response.Token.Should().Be("JWT_DEV_TOKEN");
        response.User.Should().NotBeNull();
        response.User!.Role.Should().Be(SystemRoles.Developer);
        response.User.AllowedTenantIds.Should().HaveCount(2);
    }
}

public class TenantServiceTests
{
    private readonly Mock<ITenantRepository> _tenantRepoMock = new();
    private readonly Mock<IAdminAuditRepository> _auditRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    [Fact]
    public async Task CreateAsync_UniqueCode_CreatesTenantSuccessfully()
    {
        _tenantRepoMock.Setup(r => r.ExistsCodeAsync("ACM", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new TenantService(
            _tenantRepoMock.Object,
            _auditRepoMock.Object,
            _currentUserMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<TenantService>.Instance);

        var result = await service.CreateAsync(new CreateTenantDto
        {
            Name = "Acme Corp",
            Code = "ACM",
            Prefix = "ACM",
            IsActive = true
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Code.Should().Be("ACM");
        result.Value.Prefix.Should().Be("ACM");

        _tenantRepoMock.Verify(r => r.AddAsync(It.Is<Tenant>(t => t.Code == "ACM" && t.Prefix == "ACM"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
