using Azure.Storage.Blobs;
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
    private readonly BlobServiceClient _blobServiceClient;

    public AdminMeetingsFunction(IMeetingService meetingService, BlobServiceClient blobServiceClient)
    {
        _meetingService = meetingService;
        _blobServiceClient = blobServiceClient;
    }

    [Function("AdminGetMeetings")]
    public async Task<IActionResult> GetMeetings(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/meetings")] HttpRequest req)
    {
        var query = ParseQuery(req);
        var result = await _meetingService.GetAllAsync(query);
        return new OkObjectResult(result);
    }

    [Function("AdminCreateMeeting")]
    public async Task<IActionResult> CreateMeeting(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/meetings")] HttpRequest req)
    {
        var request = await req.ReadFromJsonAsync<CreateMeetingRequest>();
        if (request == null)
            return new BadRequestObjectResult("Invalid request body");

        var result = await _meetingService.CreateAsync(request);
        return new CreatedAtRouteResult(null, new { id = result.Id }, result);
    }

    [Function("AdminUpdateMeeting")]
    public async Task<IActionResult> UpdateMeeting(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "admin/meetings/{id}")] HttpRequest req,
        Guid id)
    {
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
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "admin/meetings/{id}")] HttpRequest req,
        Guid id)
    {
        var success = await _meetingService.DeleteAsync(id);
        if (!success)
            return new NotFoundResult();

        return new NoContentResult();
    }

    [Function("AdminUploadAudio")]
    public async Task<IActionResult> UploadAudio(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/meetings/{id}/audio")] HttpRequest req,
        Guid id)
    {
        if (!req.HasFormContentType || req.Form.Files.Count == 0)
            return new BadRequestObjectResult("No file uploaded");

        var file = req.Form.Files[0];
        if (file.Length == 0)
            return new BadRequestObjectResult("Empty file");

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

        // Update meeting with blob name
        var success = await _meetingService.UpdateAudioBlobAsync(id, blobName);
        if (!success)
            return new NotFoundResult();

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

        if (DateTime.TryParse(req.Query["dateFrom"], out var dateFrom))
            query.DateFrom = dateFrom;

        if (DateTime.TryParse(req.Query["dateTo"], out var dateTo))
            query.DateTo = dateTo;

        return query;
    }
}
