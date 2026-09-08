namespace TaskBoard.Web.ViewModels;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public int StatusCode { get; set; } = 500;
    public string ErrorMessage { get; set; } = "An error occurred.";
}
