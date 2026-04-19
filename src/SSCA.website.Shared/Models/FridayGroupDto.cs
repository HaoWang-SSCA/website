namespace SSCA.website.Shared.Models;

/// <summary>
/// DTO for Friday study group (周五查经小组)
/// </summary>
public class FridayGroupDto
{
    public Guid Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string? BookEnglishName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new Friday group
/// </summary>
public class CreateFridayGroupRequest
{
    public string GroupName { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string? BookEnglishName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request model for updating a Friday group
/// </summary>
public class UpdateFridayGroupRequest
{
    public Guid Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string? BookEnglishName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
