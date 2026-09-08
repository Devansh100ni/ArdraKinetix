using System.ComponentModel.DataAnnotations;

namespace TaskBoard.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username or Email is required")]
    [Display(Name = "Username or Email")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AccessDeniedViewModel
{
    public string? Message { get; set; }
    public string? RequestedPath { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Current Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New Password is required")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
