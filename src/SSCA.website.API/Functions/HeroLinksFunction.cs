using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SSCA.website.API.Services;
using SSCA.website.Shared.Models;

namespace SSCA.website.API.Functions;

/// <summary>
/// API endpoints for Hero Links (首页动态链接)
/// </summary>
public class HeroLinksFunction
{
    private readonly IHeroLinkService _heroLinkService;
    private readonly IFileStorageService _fileStorageService;

    public HeroLinksFunction(IHeroLinkService heroLinkService, IFileStorageService fileStorageService)
    {
        _heroLinkService = heroLinkService;
        _fileStorageService = fileStorageService;
    }

    /// <summary>
    /// Get active (non-expired) hero links for public display
    /// </summary>
    [Function("GetActiveHeroLinks")]
    public async Task<IActionResult> GetActiveLinks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hero-links")] HttpRequest req)
    {
        var links = await _heroLinkService.GetActiveLinksAsync();
        return new OkObjectResult(links);
    }

    /// <summary>
    /// Get all hero links for admin management (including expired)
    /// </summary>
    [Function("AdminGetHeroLinks")]
    public async Task<IActionResult> GetAllLinks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mgmt/hero-links")] HttpRequest req)
    {
        var links = await _heroLinkService.GetAllLinksAsync();
        return new OkObjectResult(links);
    }

    /// <summary>
    /// Create a new hero link
    /// </summary>
    [Function("AdminCreateHeroLink")]
    public async Task<IActionResult> CreateHeroLink(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mgmt/hero-links")] HttpRequest req)
    {
        var request = await req.ReadFromJsonAsync<CreateHeroLinkRequest>();
        if (request == null)
            return new BadRequestObjectResult("Invalid request body");

        if (string.IsNullOrWhiteSpace(request.Text))
            return new BadRequestObjectResult("Text is required");

        if (string.IsNullOrWhiteSpace(request.ExternalUrl) && string.IsNullOrWhiteSpace(request.FileBlobName))
            return new BadRequestObjectResult("Either ExternalUrl or FileBlobName is required");

        var result = await _heroLinkService.CreateAsync(request);
        return new CreatedResult($"/api/mgmt/hero-links/{result.Id}", result);
    }

    /// <summary>
    /// Update an existing hero link
    /// </summary>
    [Function("AdminUpdateHeroLink")]
    public async Task<IActionResult> UpdateHeroLink(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "mgmt/hero-links/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var request = await req.ReadFromJsonAsync<UpdateHeroLinkRequest>();
        if (request == null)
            return new BadRequestObjectResult("Invalid request body");

        request.Id = id;
        var result = await _heroLinkService.UpdateAsync(request);
        if (result == null)
            return new NotFoundResult();

        return new OkObjectResult(result);
    }

    /// <summary>
    /// Delete a hero link
    /// </summary>
    [Function("AdminDeleteHeroLink")]
    public async Task<IActionResult> DeleteHeroLink(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "mgmt/hero-links/{id:guid}")] HttpRequest req,
        Guid id)
    {
        // First get the link to check if it has a file to delete
        var link = await _heroLinkService.GetByIdAsync(id);
        if (link == null)
            return new NotFoundResult();

        // Delete associated file if exists
        if (!string.IsNullOrEmpty(link.FileBlobName))
        {
            await _fileStorageService.DeleteFileAsync(link.FileBlobName, "hero-links");
        }

        var success = await _heroLinkService.DeleteAsync(id);
        if (!success)
            return new NotFoundResult();

        return new NoContentResult();
    }

    /// <summary>
    /// Upload a file for a hero link
    /// </summary>
    [Function("AdminUploadHeroLinkFile")]
    public async Task<IActionResult> UploadFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mgmt/hero-links/upload")] HttpRequest req)
    {
        if (!req.HasFormContentType || req.Form.Files.Count == 0)
            return new BadRequestObjectResult("No file uploaded");

        var file = req.Form.Files[0];
        if (file.Length == 0)
            return new BadRequestObjectResult("Empty file");

        // Generate unique blob name
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobName = $"{DateTime.UtcNow:yyyy-MM}/{Guid.NewGuid()}{extension}";

        using var stream = file.OpenReadStream();
        var url = await _fileStorageService.UploadFileAsync(stream, blobName, "hero-links", file.ContentType);

        return new OkObjectResult(new { blobName, url });
    }

    /// <summary>
    /// Serve a hero link file
    /// </summary>
    [Function("GetHeroLinkFile")]
    public async Task<IActionResult> GetFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hero-links/files/{*name}")] HttpRequest req,
        string name)
    {
        var stream = await _fileStorageService.DownloadFileAsync(name, "hero-links");
        if (stream == null)
        {
            return new NotFoundResult();
        }

        // Determine content type based on extension
        var extension = Path.GetExtension(name).ToLowerInvariant();
        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

        return new FileStreamResult(stream, contentType);
    }
}
