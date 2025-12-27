using System.ComponentModel.DataAnnotations;

namespace SSCA.website.Shared.Models;

/// <summary>
/// Request model for sending a contact message
/// </summary>
public class ContactMessageRequest
{
    [Required(ErrorMessage = "请输入姓名 / Name is required")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入电邮 / Email is required")]
    [EmailAddress(ErrorMessage = "请输入有效的电邮地址 / Please enter a valid email")]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    [Required(ErrorMessage = "请输入留言 / Message is required")]
    [MinLength(10, ErrorMessage = "留言至少需要10个字符 / Message must be at least 10 characters")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response model for contact message submission
/// </summary>
public class ContactMessageResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
