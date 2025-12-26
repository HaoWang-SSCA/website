using System.ComponentModel.DataAnnotations;

namespace SSCA.website.Shared.Models;

/// <summary>
/// Request model for updating an existing meeting
/// </summary>
public class UpdateMeetingRequest
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public DateTime Date { get; set; }              // 信息日期

    [Required]
    [MaxLength(100)]
    public string Speaker { get; set; } = string.Empty;  // 讲员

    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;    // 信息主题

    [MaxLength(500)]
    public string? VideoUrl { get; set; }           // YouTube/Video link

    public bool IsGospel { get; set; }              // 福音信息 flag
    public bool IsSpecialMeeting { get; set; }      // 特别聚会 flag

    [MaxLength(500)]
    public string? AudioBlobName { get; set; }      // Audio file blob name from upload
}
