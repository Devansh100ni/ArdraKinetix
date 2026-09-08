using FluentValidation;
using TaskBoard.Application.DTOs.Auth;
using TaskBoard.Application.DTOs.Comments;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.DTOs.Tenants;
using TaskBoard.Application.DTOs.Users;

namespace TaskBoard.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("Username or Email is required.")
            .MaximumLength(100).WithMessage("Identifier cannot exceed 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class CreateTenantDtoValidator : AbstractValidator<CreateTenantDto>
{
    public CreateTenantDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tenant name is required.")
            .MaximumLength(150).WithMessage("Tenant name cannot exceed 150 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Tenant code is required.")
            .MinimumLength(2).WithMessage("Tenant code must be at least 2 characters.")
            .MaximumLength(20).WithMessage("Tenant code cannot exceed 20 characters.")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Tenant code can only contain alphanumeric characters, underscores, and dashes.");

        RuleFor(x => x.Prefix)
            .NotEmpty().WithMessage("Tenant task prefix is required.")
            .MinimumLength(2).WithMessage("Prefix must be at least 2 characters.")
            .MaximumLength(10).WithMessage("Prefix cannot exceed 10 characters.")
            .Matches("^[a-zA-Z0-9]+$").WithMessage("Prefix can only contain alphanumeric characters.");
    }
}

public class UpdateTenantDtoValidator : AbstractValidator<UpdateTenantDto>
{
    public UpdateTenantDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Tenant ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tenant name is required.")
            .MaximumLength(150).WithMessage("Tenant name cannot exceed 150 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Tenant code is required.")
            .MinimumLength(2).WithMessage("Tenant code must be at least 2 characters.")
            .MaximumLength(20).WithMessage("Tenant code cannot exceed 20 characters.");

        RuleFor(x => x.Prefix)
            .NotEmpty().WithMessage("Tenant task prefix is required.")
            .MinimumLength(2).WithMessage("Prefix must be at least 2 characters.")
            .MaximumLength(10).WithMessage("Prefix cannot exceed 10 characters.");
    }
}

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.")
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Username can only contain alphanumeric characters, dots, underscores, and dashes.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.");
    }
}

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.");

        When(x => !string.IsNullOrEmpty(x.NewPassword), () =>
        {
            RuleFor(x => x.NewPassword)
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
        });

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.");
    }
}

public class CreateTaskDtoValidator : AbstractValidator<CreateTaskDto>
{
    public CreateTaskDtoValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(200).WithMessage("Task title cannot exceed 200 characters.");

        RuleFor(x => x.StatusId)
            .NotEmpty().WithMessage("Status is required.");

        RuleFor(x => x.PriorityId)
            .NotEmpty().WithMessage("Priority is required.");

        RuleFor(x => x.EstimatedHours)
            .GreaterThanOrEqualTo(0).When(x => x.EstimatedHours.HasValue)
            .WithMessage("Estimated hours cannot be negative.");

        When(x => x.StartedOn.HasValue && x.EndOn.HasValue, () =>
        {
            RuleFor(x => x.EndOn)
                .GreaterThanOrEqualTo(x => x.StartedOn)
                .WithMessage("End date must be greater than or equal to Start date.");
        });
    }
}

public class UpdateTaskDtoValidator : AbstractValidator<UpdateTaskDto>
{
    public UpdateTaskDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Task ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(200).WithMessage("Task title cannot exceed 200 characters.");

        RuleFor(x => x.StatusId)
            .NotEmpty().WithMessage("Status is required.");

        RuleFor(x => x.PriorityId)
            .NotEmpty().WithMessage("Priority is required.");

        RuleFor(x => x.EstimatedHours)
            .GreaterThanOrEqualTo(0).When(x => x.EstimatedHours.HasValue)
            .WithMessage("Estimated hours cannot be negative.");

        When(x => x.StartedOn.HasValue && x.EndOn.HasValue, () =>
        {
            RuleFor(x => x.EndOn)
                .GreaterThanOrEqualTo(x => x.StartedOn)
                .WithMessage("End date must be greater than or equal to Start date.");
        });
    }
}

public class CreateTaskCommentDtoValidator : AbstractValidator<CreateTaskCommentDto>
{
    public CreateTaskCommentDtoValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Task ID is required.");
        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Comment text is required.")
            .MaximumLength(4000).WithMessage("Comment cannot exceed 4000 characters.");
    }
}

public class MoveTaskDtoValidator : AbstractValidator<MoveTaskDto>
{
    public MoveTaskDtoValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Task ID is required.");
        RuleFor(x => x.TargetStatusId).NotEmpty().WithMessage("Target status ID is required.");
    }
}
