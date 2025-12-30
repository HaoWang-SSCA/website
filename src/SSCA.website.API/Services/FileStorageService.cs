using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace SSCA.website.API.Services;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName, string contentType);
    Task<Stream?> DownloadFileAsync(string fileName, string containerName);
    Task<bool> DeleteFileAsync(string fileName, string containerName);
}

public class FileStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public FileStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException("AzureStorage connection string is missing");
        }
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName, string contentType)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var blobClient = containerClient.GetBlobClient(fileName);
        
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(fileStream, options);

        return blobClient.Uri.ToString();
    }

    public async Task<Stream?> DownloadFileAsync(string fileName, string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);

        if (!await blobClient.ExistsAsync())
        {
            return null;
        }

        var response = await blobClient.DownloadStreamingAsync();
        return response.Value.Content;
    }

    public async Task<bool> DeleteFileAsync(string fileName, string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);
        return await blobClient.DeleteIfExistsAsync();
    }
}
