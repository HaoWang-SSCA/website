using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
/// For Azure, consider using SendGrid or Azure Communication Services
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
            var recipientEmail = _configuration["ContactEmail"] ?? "tech@ssca-bc.org";
            
            _logger.LogInformation(
                "Contact form submission received from {Name} ({Email})",
                request.Name,
                request.Email
            );

            // Log the message details (in production, you would send an actual email here)
            // Options for actual email sending:
            // 1. SendGrid - Add Microsoft.Azure.Functions.Worker.Extensions.SendGrid package
            // 2. Azure Communication Services
            // 3. SMTP with System.Net.Mail

            // For now, log the message content for processing
            _logger.LogInformation(
                "Contact Message:\n" +
                "From: {Name} <{Email}>\n" +
                "Phone: {Phone}\n" +
                "Message: {Message}",
                request.Name,
                request.Email,
                request.Phone ?? "Not provided",
                request.Message
            );

            // Simulate async operation
            await Task.CompletedTask;

            // In production, implement actual email sending here
            // Example with SendGrid:
            // var client = new SendGridClient(apiKey);
            // var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, htmlContent);
            // var response = await client.SendEmailAsync(msg);
            // return response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact message from {Email}", request.Email);
            return false;
        }
    }
}
