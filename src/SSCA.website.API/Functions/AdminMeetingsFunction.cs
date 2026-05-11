using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SSCA.website.API.Services;
using SSCA.website.Shared.Models;

namespace SSCA.website.API.Functions;

/// <summary>
/// Admin API endpoints for meeting CRUD operations (requires authentication)
/// </summary>
public class AdminMeetingsFunction
{
    private readonly IMeetingService _meetingService;
    private readonly IAdminAuthorizationService _adminAuthorization;
    private readonly BlobServiceClient? _blobServiceClient;
    private const long MaxUploadSizeBytes = 50 * 1024 * 1024;

    public AdminMeetingsFunction(
        IMeetingService meetingService,
        IAdminAuthorizationService adminAuthorization,
        BlobServiceClient? blobServiceClient = null)
    {
        _meetingService = meetingService;
        _adminAuthorization = adminAuthorization;
        _blobServiceClient = blobServiceClient;
    }

    private IActionResult? RequireAdmin(HttpRequest req)
    {
        return _adminAuthorization.IsAdmin(req)
            ? null
            : new UnauthorizedObjectResult("Admin authorization required.");
    }

    // Simple test endpoint to diagnose routing
    [Function("AdminTest")]
    public IActionResult Test(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mgmt/test")] HttpRequest req)
    {
        if (RequireAdmin(req) is { } unauthorized) return unauthorized;

        return new OkObjectResult(new { message = "Admin API is working!", timestamp = DateTime.UtcNow });
    }

    [Function("AdminGetMeetings")]
    public async Task<IActionResult> GetMeetings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mgmt/meetings")] HttpRequest req)
    {
        if (RequireAdmin(req) is { } unauthorized) return unauthorized;

        var query = ParseQuery(req);
        var result = await _meetingService.GetAllAsync(query);
        return new OkObjectResult(result);
    }

    [Function("AdminCreateMeeting")]
    public async Task<IActionResult> CreateMeeting(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mgmt/meetings")] HttpRequest req)
    {
        if (RequireAdmin(req) is { } unauthorized) return unauthorized;

        var request = await req.ReadFromJsonAsync<CreateMeetingRequest>();
        if (request == null)
            return new BadRequestObjectResult("Invalid request body");

        var result = await _meetingService.CreateAsync(request);
        return new CreatedResult($"/api/mgmt/meetings/{result.Id}", result);
    }

    [Function("AdminUpdateMeeting")]
    public async Task<IActionResult> UpdateMeeting(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "mgmt/meetings/{id:guid}")] HttpRequest req,
        Guid id)
    {
        if (RequireAdmin(req) is { } unauthorized) return unauthorized;

        var request = await req.ReadFromJsonAsync<UpdateMeetingRequest>();
        if (request == null)
            return new BadRequestObjectResult("Invalid request body");

        request.Id = id;
        var result = await _meetingService.UpdateAsync(request);
        if (result == null)
            return new NotFoundResult();

        return new OkObjectResult(result);
    }

    [Function("AdminDeleteMeeting")]
    public async Task<IActionResult> DeleteMeeting(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "mgmt/meetings/{id:guid}")] HttpRequest req,
        Guid id)
    {
        if (RequireAdmin(req) is { } unauthorized) return unauthorized;

        var success = await _meetingService.DeleteAsync(id);
        if (!success)
            return new NotFoundResult();

        return new NoContentResult();
    }

    /// <summary>
    /// Upload audio file to blob storage without requiring a meeting ID.
    /// The returned blobName should be included in the create/update meeting request.
    /// </summary>
    [Function("AdminUploadAudio")]
    public async Task<IActionResult> UploadAudio(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mgmt/audio-upload")] HttpRequest req)
    {
        if (RequireAdmin(req) is { } unauthorized) return unauthorized;

        if (_blobServiceClient == null)
            return new StatusCodeResult(503); // Service unavailable if blob storage not configured

        if (!req.HasFormContentType || req.Form.Files.Count == 0)
            return new BadRequestObjectResult("No file uploaded");

        var file = req.Form.Files[0];
        if (file.Length == 0)
            return new BadRequestObjectResult("Empty file");
        if (file.Length > MaxUploadSizeBytes)
            return new BadRequestObjectResult($"File too large. Max size is {MaxUploadSizeBytes / 1024 / 1024}MB");

        // Validate file type
        var allowedExtensions = new[] { ".mp3", ".wav", ".m4a", ".ogg" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return new BadRequestObjectResult($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");

        // Generate blob name: {year}/{guid}-{filename}
        var year = DateTime.UtcNow.Year;
        var blobName = $"{year}/{Guid.NewGuid()}-{Path.GetFileName(file.FileName)}";

        // Upload to Blob Storage
        var containerClient = _blobServiceClient.GetBlobContainerClient("audio-files");
        await containerClient.CreateIfNotExistsAsync();
        
        var blobClient = containerClient.GetBlobClient(blobName);
        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true);

        return new OkObjectResult(new { blobName, url = blobClient.Uri.ToString() });
    }

    /// <summary>
    /// Upload PowerPoint file to blob storage without requiring a meeting ID.
    /// The returned blobName should be included in the create/update meeting request.
    /// </summary>
    [Function("AdminUploadPowerPoint")]
    public async Task<IActionResult> UploadPowerPoint(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mgmt/powerpoint-upload")] HttpRequest req)
    {
        if (RequireAdmin(req) is { } unauthorized) return unauthorized;

        if (_blobServiceClient == null)
            return new StatusCodeResult(503); // Service unavailable if blob storage not configured

        if (!req.HasFormContentType || req.Form.Files.Count == 0)
            return new BadRequestObjectResult("No file uploaded");

        var file = req.Form.Files[0];
        if (file.Length == 0)
            return new BadRequestObjectResult("Empty file");
        if (file.Length > MaxUploadSizeBytes)
            return new BadRequestObjectResult($"File too large. Max size is {MaxUploadSizeBytes / 1024 / 1024}MB");

        // Validate file type
        var allowedExtensions = new[] { ".ppt", ".pptx" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return new BadRequestObjectResult($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");

        // Generate blob name: {year}/{guid}-{filename}
        var year = DateTime.UtcNow.Year;
        var blobName = $"{year}/{Guid.NewGuid()}-{Path.GetFileName(file.FileName)}";

        // Upload to Blob Storage
        var containerClient = _blobServiceClient.GetBlobContainerClient("powerpoint-files");
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var blobClient = containerClient.GetBlobClient(blobName);
        using var stream = file.OpenReadStream();
        var contentType = extension == ".pptx"
            ? "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            : "application/vnd.ms-powerpoint";
        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });

        return new OkObjectResult(new { blobName, url = blobClient.Uri.ToString() });
    }

    private static MeetingSearchQuery ParseQuery(HttpRequest req)
    {
        var query = new MeetingSearchQuery();

        if (int.TryParse(req.Query["page"], out var page))
            query.Page = page;

        if (int.TryParse(req.Query["pageSize"], out var pageSize))
            query.PageSize = Math.Min(pageSize, 50);

        query.Speaker = req.Query["speaker"];
        query.Topic = req.Query["topic"];
        query.Scripture = req.Query["scripture"];

        if (DateTime.TryParse(req.Query["dateFrom"], out var dateFrom))
            query.DateFrom = dateFrom;

        if (DateTime.TryParse(req.Query["dateTo"], out var dateTo))
            query.DateTo = dateTo;

        return query;
    }
}
