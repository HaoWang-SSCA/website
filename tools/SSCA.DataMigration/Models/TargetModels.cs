namespace SSCA.DataMigration.Models;

/// <summary>
/// Target database model - matches MessageMeeting in SSCA.website.API
/// </summary>
public class TargetMessageMeeting
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }                     // 信息日期
    public string Speaker { get; set; } = string.Empty;   // 讲员
    public string Topic { get; set; } = string.Empty;     // 信息主题
    public string? AudioBlobName { get; set; }            // Azure Blob Storage path
    public string? VideoUrl { get; set; }                 // YouTube/Video link
    public bool IsGospel { get; set; }                    // 福音信息 flag
    public bool IsSpecialMeeting { get; set; }            // 特别聚会 flag
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
