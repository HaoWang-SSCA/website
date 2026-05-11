using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SSCA.website.API.Services;

public interface IAdminAuthorizationService
{
    bool IsAdmin(HttpRequest request);
}

public sealed class AdminAuthorizationService : IAdminAuthorizationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminAuthorizationService> _logger;

    public AdminAuthorizationService(IConfiguration configuration, ILogger<AdminAuthorizationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsAdmin(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("x-ms-client-principal", out var encodedPrincipal) ||
            string.IsNullOrWhiteSpace(encodedPrincipal))
        {
            return false;
        }

        try
        {
            var principalJson = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrincipal!));
            var principal = JsonSerializer.Deserialize<ClientPrincipal>(principalJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (principal is null)
            {
                return false;
            }

            if (principal.UserRoles?.Any(role => string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)) == true)
            {
                return true;
            }

            var allowedEmails = (_configuration["Admin:AllowedEmails"] ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return allowedEmails.Any(email =>
                string.Equals(email, principal.UserDetails, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Static Web Apps client principal for admin authorization.");
            return false;
        }
    }

    private sealed class ClientPrincipal
    {
        public string? UserDetails { get; set; }
        public string[]? UserRoles { get; set; }
    }
}
