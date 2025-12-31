using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SSCA.website.API.Services;

namespace SSCA.website.API.Functions;

public class BulletinFunction
{
    private readonly IFileStorageService _fileStorageService;

    public BulletinFunction(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    [Function("AdminUploadBulletin")]
    public async Task<IActionResult> UploadBulletin(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mgmt/bulletin-upload")] HttpRequest req)
    {
        if (!req.HasFormContentType || req.Form.Files.Count == 0)
            return new BadRequestObjectResult("No file uploaded");

        var file = req.Form.Files[0];
        if (file.Length == 0)
            return new BadRequestObjectResult("Empty file");

        if (Path.GetExtension(file.FileName).ToLowerInvariant() != ".pdf")
            return new BadRequestObjectResult("Only PDF files are allowed");

        // We use a fixed name for the Sunday Bulletin so the home page link remains stable
        // Alternatively, we could include the date in the filename, but fixed is easier for "Latest"
        var blobName = "SundayBulletin.pdf";
        
        using var stream = file.OpenReadStream();
        var url = await _fileStorageService.UploadFileAsync(stream, blobName, "bulletin", "application/pdf");

        return new OkObjectResult(new { url });
    }

    [Function("GetBulletin")]
    public async Task<IActionResult> GetBulletin(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bulletin/{*name}")] HttpRequest req,
        string name)
    {
        var stream = await _fileStorageService.DownloadFileAsync(name, "bulletin");
        if (stream == null)
        {
            return new NotFoundResult();
        }

        return new FileStreamResult(stream, "application/pdf");
    }
}
