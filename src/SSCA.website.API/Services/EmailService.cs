using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SSCA.website.Shared.Models;

namespace SSCA.website.API.Services;

/// <summary>
/// Interface for email service
/// </summary>
public interface IEmailService
{
    Task<bool> SendContactMessageAsync(ContactMessageRequest request);
}

/// <summary>
/// Email service implementation using SMTP
/// Supports Microsoft 365 (Office 365) and Gmail SMTP
/// 
/// Required Configuration:
/// 
/// For Microsoft 365:
/// - Smtp:Host = smtp.office365.com
/// - Smtp:Port = 587
/// - Smtp:Username = yourname@yourdomain.com
/// - Smtp:Password = [Password or App Password if MFA enabled]
/// - Smtp:FromEmail = yourname@yourdomain.com
/// - Smtp:FromName = SSCA-BC Website
/// - ContactEmail = recipient@domain.com
/// 
/// For Gmail:
/// - Smtp:Host = smtp.gmail.com
/// - Smtp:Port = 587
/// - Smtp:Username = yourname@gmail.com
/// - Smtp:Password = [Gmail App Password - NOT regular password]
/// - Smtp:FromEmail = yourname@gmail.com
/// - Smtp:FromName = SSCA-BC Website
/// - ContactEmail = recipient@domain.com
/// 
/// Note: For M365, ensure "Authenticated SMTP" is enabled for the mailbox
/// in Microsoft 365 Admin Center > Users > Mail > Manage email apps
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> SendContactMessageAsync(ContactMessageRequest request)
    {
        try
        {
            var smtpHost = _configuration["Smtp:Host"] ?? "smtp.office365.com";
            var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var smtpUsername = _configuration["Smtp:Username"] ?? "hao.wang@team.ssca-bc.org";
            var smtpPassword = _configuration["Smtp:Password"] ?? "Skight163#";
            var fromEmail = _configuration["Smtp:FromEmail"] ?? "hao.wang@team.ssca-bc.org";
            var fromName = _configuration["Smtp:FromName"] ?? "SSCA-BC Website";
            var recipientEmail = _configuration["ContactEmail"] ?? "tech@ssca-bc.org";

            // Check if SMTP is configured
            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("SMTP not configured. Logging message instead of sending email.");
                LogContactMessage(request);
                return true; // Return true so user sees success (message is logged)
            }

            // Create the email message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress("SSCA-BC", recipientEmail));
            message.ReplyTo.Add(new MailboxAddress(request.Name, request.Email));
            message.Subject = $"网站留言 - 来自 {request.Name}";

            // Build email body
            var bodyBuilder = new BodyBuilder
            {
                TextBody = BuildPlainTextBody(request),
                HtmlBody = BuildHtmlBody(request)
            };
            message.Body = bodyBuilder.ToMessageBody();

            // Send via SMTP
            using var client = new SmtpClient();
            
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "Contact email sent successfully from {Name} ({Email})",
                request.Name,
                request.Email
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact email from {Email}", request.Email);
            return false;
        }
    }

    private void LogContactMessage(ContactMessageRequest request)
    {
        _logger.LogInformation(
            "Contact form submission received:\n" +
            "Name: {Name}\n" +
            "Email: {Email}\n" +
            "Phone: {Phone}\n" +
            "Message: {Message}",
            request.Name,
            request.Email,
            request.Phone ?? "Not provided",
            request.Message
        );
    }

    private static string BuildPlainTextBody(ContactMessageRequest request)
    {
        return $@"网站留言 / Website Contact Form

姓名 / Name: {request.Name}
电邮 / Email: {request.Email}
电话 / Phone: {request.Phone ?? "未提供 / Not provided"}

留言 / Message:
{request.Message}

---
此邮件由 SSCA-BC 网站自动发送
This email was sent automatically from the SSCA-BC website";
    }

    private static string BuildHtmlBody(ContactMessageRequest request)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #137fec, #1a5fb4); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9fafb; padding: 20px; border: 1px solid #e5e7eb; }}
        .field {{ margin-bottom: 15px; }}
        .label {{ font-weight: 600; color: #6b7280; font-size: 14px; }}
        .value {{ margin-top: 4px; }}
        .message-box {{ background: white; padding: 15px; border-radius: 8px; border: 1px solid #e5e7eb; margin-top: 10px; }}
        .footer {{ background: #f3f4f6; padding: 15px; font-size: 12px; color: #6b7280; border-radius: 0 0 8px 8px; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin: 0;'>📬 网站留言 / Website Contact</h2>
        </div>
        <div class='content'>
            <div class='field'>
                <div class='label'>姓名 / Name</div>
                <div class='value'>{System.Web.HttpUtility.HtmlEncode(request.Name)}</div>
            </div>
            <div class='field'>
                <div class='label'>电邮 / Email</div>
                <div class='value'><a href='mailto:{System.Web.HttpUtility.HtmlEncode(request.Email)}'>{System.Web.HttpUtility.HtmlEncode(request.Email)}</a></div>
            </div>
            <div class='field'>
                <div class='label'>电话 / Phone</div>
                <div class='value'>{System.Web.HttpUtility.HtmlEncode(request.Phone ?? "未提供 / Not provided")}</div>
            </div>
            <div class='field'>
                <div class='label'>留言 / Message</div>
                <div class='message-box'>{System.Web.HttpUtility.HtmlEncode(request.Message).Replace("\n", "<br>")}</div>
            </div>
        </div>
        <div class='footer'>
            此邮件由 SSCA-BC 网站自动发送<br>
            This email was sent automatically from the SSCA-BC website
        </div>
    </div>
</body>
</html>";
    }
}

