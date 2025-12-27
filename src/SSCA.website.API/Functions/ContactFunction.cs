using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SSCA.website.API.Services;
using SSCA.website.Shared.Models;

namespace SSCA.website.API.Functions;

/// <summary>
/// Public API endpoint for contact form submissions
/// </summary>
public class ContactFunction
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactFunction> _logger;

    public ContactFunction(IEmailService emailService, ILogger<ContactFunction> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [Function("SendContactMessage")]
    public async Task<IActionResult> SendContactMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequest req)
    {
        try
        {
            var request = await req.ReadFromJsonAsync<ContactMessageRequest>();

            if (request == null)
            {
                return new BadRequestObjectResult(new ContactMessageResponse
                {
                    Success = false,
                    Message = "Invalid request body"
                });
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new BadRequestObjectResult(new ContactMessageResponse
                {
                    Success = false,
                    Message = "请输入姓名 / Name is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new BadRequestObjectResult(new ContactMessageResponse
                {
                    Success = false,
                    Message = "请输入电邮 / Email is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return new BadRequestObjectResult(new ContactMessageResponse
                {
                    Success = false,
                    Message = "请输入留言 / Message is required"
                });
            }

            if (request.Message.Length < 10)
            {
                return new BadRequestObjectResult(new ContactMessageResponse
                {
                    Success = false,
                    Message = "留言至少需要10个字符 / Message must be at least 10 characters"
                });
            }

            // Send the message
            var success = await _emailService.SendContactMessageAsync(request);

            if (success)
            {
                _logger.LogInformation("Contact message sent successfully from {Email}", request.Email);
                return new OkObjectResult(new ContactMessageResponse
                {
                    Success = true,
                    Message = "感谢您的留言！我们会尽快回复您。/ Thank you for your message!"
                });
            }
            else
            {
                return new ObjectResult(new ContactMessageResponse
                {
                    Success = false,
                    Message = "发送消息失败，请稍后重试。/ Failed to send message, please try again later."
                })
                {
                    StatusCode = 500
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing contact form submission");
            return new ObjectResult(new ContactMessageResponse
            {
                Success = false,
                Message = "服务器错误，请稍后重试。/ Server error, please try again later."
            })
            {
                StatusCode = 500
            };
        }
    }
}
