namespace SSCA.DataMigration.Models;

/// <summary>
/// Source database model - represents ssca_sunday_msg table from MySQL
/// </summary>
public class SourceSundayMessage
{
    public int Id { get; set; }
    public string Date { get; set; } = string.Empty;      // char(11), e.g., "2024-01-07"
    public int DateTs { get; set; }                        // Unix timestamp
    public string Speaker { get; set; } = string.Empty;   // 讲员
    public string Theme { get; set; } = string.Empty;     // 信息主题
    public int Gospel { get; set; }                        // 0 or 1, 福音信息 flag
    public string AudioFile { get; set; } = string.Empty; // Audio filename, e.g., "2018_07_29.mp3"
    public string? YoutubeLink { get; set; }               // YouTube video URL
}

/// <summary>
/// Source database model - represents ssca_special_msg table from MySQL
/// </summary>
public class SourceSpecialMessage
{
    public int Id { get; set; }
    public string Date { get; set; } = string.Empty;      // char(11)
    public int DateTs { get; set; }                        // Unix timestamp
    public string Speaker { get; set; } = string.Empty;   // 讲员
    public string Theme { get; set; } = string.Empty;     // 信息主题
    public int Gospel { get; set; }                        // 0 or 1
    public string AudioFile { get; set; } = string.Empty; // Audio filename
    public string? YoutubeLink { get; set; }               // YouTube video URL
}
