using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.Features.Tasks;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Exceptions;
using Xunit;

namespace TaskBoard.Application.Tests;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepoMock = new();
    private readonly Mock<ITenantRepository> _tenantRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ITaskStatusRepository> _statusRepoMock = new();
    private readonly Mock<ITaskPriorityRepository> _priorityRepoMock = new();
    private readonly Mock<ITaskAuditRepository> _taskAuditRepoMock = new();
    private readonly Mock<IAdminAuditRepository> _adminAuditRepoMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<ITaskNumberGenerator> _taskNumberGenMock = new();
    private readonly Mock<IFileStorageService> _fileStorageMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private TaskService CreateService()
    {
        return new TaskService(
            _taskRepoMock.Object,
            _tenantRepoMock.Object,
            _userRepoMock.Object,
            _statusRepoMock.Object,
            _priorityRepoMock.Object,
            _taskAuditRepoMock.Object,
            _tenantContextMock.Object,
            _currentUserMock.Object,
            _taskNumberGenMock.Object,
            _fileStorageMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<TaskService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_GeneratesTenantPrefixedTaskNumber()
    {
        var tenantId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var priorityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _tenantContextMock.Setup(c => c.IsGlobalAdmin).Returns(true);
        _tenantContextMock.Setup(c => c.HasAccessToTenant(tenantId)).Returns(true);
        _currentUserMock.Setup(u => u.UserId).Returns(userId);

        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = tenantId, Name = "Acme", Code = "ACM", Prefix = "ACM", IsActive = true });

        _statusRepoMock.Setup(r => r.GetByIdAsync(statusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskStatusItem { Id = statusId, Name = "To Do", Code = "TODO", IsActive = true });

        _priorityRepoMock.Setup(r => r.GetByIdAsync(priorityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskPriority { Id = priorityId, Name = "High", Code = "HIGH", IsActive = true });

        _taskNumberGenMock.Setup(g => g.GenerateNextTaskNumberAsync(tenantId, "ACM", It.IsAny<CancellationToken>()))
            .ReturnsAsync("ACM-000001");

        var service = CreateService();

        var result = await service.CreateAsync(new CreateTaskDto
        {
            TenantId = tenantId,
            Title = "Setup Evolve Migrations",
            StatusId = statusId,
            PriorityId = priorityId
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TaskNumber.Should().Be("ACM-000001");

        _taskRepoMock.Verify(r => r.AddAsync(It.Is<TaskItem>(t => t.TaskNumber == "ACM-000001" && t.TenantId == tenantId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UnauthorizedTenantAccess_ThrowsTenantAccessDeniedException()
    {
        var targetTenantId = Guid.NewGuid();
        _tenantContextMock.Setup(c => c.IsGlobalAdmin).Returns(false);
        _tenantContextMock.Setup(c => c.HasAccessToTenant(targetTenantId)).Returns(false);

        var service = CreateService();

        var act = async () => await service.CreateAsync(new CreateTaskDto
        {
            TenantId = targetTenantId,
            Title = "Unauthorized Task",
            StatusId = Guid.NewGuid(),
            PriorityId = Guid.NewGuid()
        });

        await act.Should().ThrowAsync<TenantAccessDeniedException>();
    }
}
