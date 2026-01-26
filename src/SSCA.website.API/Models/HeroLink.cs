using System.ComponentModel.DataAnnotations;

namespace SSCA.website.API.Models;

/// <summary>
/// Hero Link entity for database (首页动态链接)
/// </summary>
public class HeroLink
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Display text for the link (链接显示文字)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// External URL or null if using uploaded file (外部链接地址)
    /// </summary>
    [MaxLength(500)]
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// Uploaded file blob name (上传文件存储名称)
    /// </summary>
    [MaxLength(500)]
    public string? FileBlobName { get; set; }

    /// <summary>
    /// When the link should stop displaying (过期日期)
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Display order - smaller numbers appear first
    /// </summary>
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
