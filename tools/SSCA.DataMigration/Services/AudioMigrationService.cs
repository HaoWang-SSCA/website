using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Serilog;

namespace SSCA.DataMigration.Services;

/// <summary>
/// Service for downloading audio files from source website and uploading to Azure Blob Storage
/// </summary>
public class AudioMigrationService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly BlobContainerClient _containerClient;
    private readonly string _sourceWebsiteDomain;
    private readonly string _sundayMessageAudioPath;
    private readonly string _specialMessageAudioPath;

    public AudioMigrationService(
        string sourceWebsiteDomain, 
        string sundayMessageAudioPath,
        string specialMessageAudioPath,
        string azureStorageConnectionString, 
        string containerName)
    {
        _sourceWebsiteDomain = sourceWebsiteDomain.TrimEnd('/');
        _sundayMessageAudioPath = sundayMessageAudioPath.TrimEnd('/');
        _specialMessageAudioPath = specialMessageAudioPath.TrimEnd('/');
        
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10); // Large files may take time

        var blobServiceClient = new BlobServiceClient(azureStorageConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    /// <summary>
    /// Test connection to Azure Blob Storage
    /// </summary>
    public async Task<bool> TestStorageConnectionAsync()
    {
        try
        {
            await _containerClient.CreateIfNotExistsAsync();
            Log.Information("Successfully connected to Azure Blob Storage container: {Container}", 
                _containerClient.Name);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to connect to Azure Blob Storage");
            return false;
        }
    }

    /// <summary>
    /// Test if source website is accessible
    /// </summary>
    public async Task<bool> TestSourceWebsiteAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(_sourceWebsiteDomain);
            Log.Information("Source website accessible: {Url}, Status: {Status}", 
                _sourceWebsiteDomain, response.StatusCode);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to access source website: {Url}", _sourceWebsiteDomain);
            return false;
        }
    }

    /// <summary>
    /// Compose the source audio URL for a Sunday message
    /// </summary>
    public string ComposeSundayAudioUrl(string audioFileName)
    {
        if (string.IsNullOrEmpty(audioFileName)) return string.Empty;
        return $"{_sourceWebsiteDomain}{_sundayMessageAudioPath}/{audioFileName}";
    }

    /// <summary>
    /// Compose the source audio URL for a Special message
    /// </summary>
    public string ComposeSpecialAudioUrl(string audioFileName)
    {
        if (string.IsNullOrEmpty(audioFileName)) return string.Empty;
        return $"{_sourceWebsiteDomain}{_specialMessageAudioPath}/{audioFileName}";
    }

    /// <summary>
    /// Compose the target blob name for an audio file
    /// </summary>
    public string ComposeBlobName(string sourceTable, string audioFileName, DateTime date)
    {
        // Organize files by type and year: sunday/2024/filename.mp3 or special/2024/filename.mp3
        var folder = sourceTable == "sunday" ? "sunday" : "special";
        var year = date.Year;
        return $"{folder}/{year}/{audioFileName}";
    }

    /// <summary>
    /// Check if a blob already exists in target storage
    /// </summary>
    public async Task<bool> BlobExistsAsync(string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        return await blobClient.ExistsAsync();
    }

    /// <summary>
    /// Migrate an audio file from source URL to Azure Blob Storage
    /// </summary>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<(bool Success, long BytesUploaded, string? Error)> MigrateAudioFileAsync(
        string sourceUrl, 
        string targetBlobName,
        bool overwrite = false)
    {
        try
        {
            // Check if blob already exists
            if (!overwrite && await BlobExistsAsync(targetBlobName))
            {
                Log.Debug("Blob already exists, skipping: {BlobName}", targetBlobName);
                return (true, 0, null);
            }

            // Download from source
            Log.Debug("Downloading: {Url}", sourceUrl);
            using var response = await _httpClient.GetAsync(sourceUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = $"Failed to download: HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                Log.Warning(error);
                return (false, 0, error);
            }

            var contentLength = response.Content.Headers.ContentLength ?? 0;
            Log.Debug("Downloaded {Bytes} bytes", contentLength);

            // Upload to Azure Blob
            await using var stream = await response.Content.ReadAsStreamAsync();
            var blobClient = _containerClient.GetBlobClient(targetBlobName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = GetContentType(targetBlobName)
            };

            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            Log.Information("Uploaded: {BlobName} ({Bytes} bytes)", targetBlobName, contentLength);
            return (true, contentLength, null);
        }
        catch (Exception ex)
        {
            var error = $"Migration failed: {ex.Message}";
            Log.Error(ex, "Failed to migrate audio file: {Source} -> {Target}", sourceUrl, targetBlobName);
            return (false, 0, error);
        }
    }

    /// <summary>
    /// Get content type based on file extension
    /// </summary>
    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".ogg" => "audio/ogg",
            ".wma" => "audio/x-ms-wma",
            _ => "application/octet-stream"
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
