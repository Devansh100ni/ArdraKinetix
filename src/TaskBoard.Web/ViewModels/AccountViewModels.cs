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
