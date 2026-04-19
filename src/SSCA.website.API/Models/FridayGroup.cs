using System.ComponentModel.DataAnnotations;

namespace SSCA.website.API.Models;

/// <summary>
/// Friday study group entity (周五查经小组)
/// </summary>
public class FridayGroup
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Group name, e.g. "中文组", "福音组", "English" (小组名称)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Book being studied in Chinese, e.g. "罗马书" (学习书卷中文名)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string BookName { get; set; } = string.Empty;

    /// <summary>
    /// Book being studied in English, e.g. "Romans" (学习书卷英文名)
    /// </summary>
    [MaxLength(200)]
    public string? BookEnglishName { get; set; }

    /// <summary>
    /// Display order - smaller numbers appear first
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this group is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
