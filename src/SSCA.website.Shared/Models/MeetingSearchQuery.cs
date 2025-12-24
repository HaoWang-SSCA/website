namespace SSCA.website.Shared.Models;

/// <summary>
/// Query parameters for meeting search with pagination
/// </summary>
public class MeetingSearchQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Speaker { get; set; }
    public string? Topic { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
