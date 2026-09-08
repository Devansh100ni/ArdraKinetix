using FluentAssertions;
using TaskBoard.Domain.Entities;
using TaskBoard.Infrastructure.Authentication;
using Xunit;
using Xunit.Abstractions;

namespace TaskBoard.Infrastructure.Tests;

public class PasswordHasherTests
{
    private readonly ITestOutputHelper _output;

    public PasswordHasherTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void HashAndVerify_ValidPassword_ReturnsTrue()
    {
        var hasher = new PasswordHasherService();
        var user = new User { Id = Guid.NewGuid(), Username = "admin" };

        var hash = hasher.HashPassword(user, "AdminPassword123!");
        _output.WriteLine($"Admin Hash: {hash}");

        var devHash = hasher.HashPassword(user, "Developer123!");
        _output.WriteLine($"Dev Hash: {devHash}");

        var tenantHash = hasher.HashPassword(user, "TenantUser123!");
        _output.WriteLine($"Tenant Hash: {tenantHash}");

        var isValid = hasher.VerifyPassword(user, hash, "AdminPassword123!");
        var isInvalid = hasher.VerifyPassword(user, hash, "WrongPassword");

        isValid.Should().BeTrue();
        isInvalid.Should().BeFalse();
    }
}
