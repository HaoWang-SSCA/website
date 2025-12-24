namespace SSCA.website.Shared.Models;

/// <summary>
/// DTO for Message Meeting (讲道信息)
/// </summary>
public class MessageMeetingDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }              // 信息日期
    public string Speaker { get; set; } = string.Empty;  // 讲员
    public string Topic { get; set; } = string.Empty;    // 信息主题
    public string? AudioUrl { get; set; }           // Audio file URL (from Blob Storage)
    public string? VideoUrl { get; set; }           // YouTube/Video link
    public bool IsGospel { get; set; }              // 福音信息 flag
    public bool IsSpecialMeeting { get; set; }      // 特别聚会 flag
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
