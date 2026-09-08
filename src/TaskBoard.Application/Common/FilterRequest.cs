namespace TaskBoard.Application.Common;

public class FilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}
