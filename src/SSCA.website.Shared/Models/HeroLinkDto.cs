namespace SSCA.website.Shared.Models;

/// <summary>
/// DTO for Hero Link data transfer between API and UI
/// 首页动态链接数据传输对象
/// </summary>
public class HeroLinkDto
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Display text for the link (链接显示文字)
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// External URL or null if using uploaded file (外部链接地址)
    /// </summary>
    public string? ExternalUrl { get; set; }
    
    /// <summary>
    /// Uploaded file blob name (上传文件存储名称)
    /// </summary>
    public string? FileBlobName { get; set; }
    
    /// <summary>
    /// When the link should stop displaying (过期日期)
    /// </summary>
    public DateTime ExpiryDate { get; set; }
    
    /// <summary>
    /// Display order (smaller numbers appear first)
    /// </summary>
    public int DisplayOrder { get; set; }
    
    /// <summary>
    /// Whether link is currently active (based on expiry date)
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new hero link
/// </summary>
public class CreateHeroLinkRequest
{
    public string Text { get; set; } = string.Empty;
    public string? ExternalUrl { get; set; }
    public string? FileBlobName { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Request model for updating an existing hero link
/// </summary>
public class UpdateHeroLinkRequest
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? ExternalUrl { get; set; }
    public string? FileBlobName { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DisplayOrder { get; set; }
}
