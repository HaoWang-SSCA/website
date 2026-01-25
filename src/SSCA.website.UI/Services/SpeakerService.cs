using System.Net.Http.Json;

namespace SSCA.website.UI.Services;

/// <summary>
/// Service that caches speakers list on the client side to minimize API calls.
/// The cache persists for the lifetime of the application session.
/// </summary>
public class SpeakerService
{
    private readonly HttpClient _http;
    private List<string>? _cachedSpeakers;
    private Task<List<string>>? _loadingTask;

    public SpeakerService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Gets the list of distinct speakers. Uses cached data if available.
    /// </summary>
    public async Task<List<string>> GetSpeakersAsync()
    {
        // Return cached data if available
        if (_cachedSpeakers != null)
        {
            return _cachedSpeakers;
        }

        // If already loading, wait for the existing task
        if (_loadingTask != null)
        {
            return await _loadingTask;
        }

        // Start loading
        _loadingTask = LoadSpeakersFromApiAsync();
        
        try
        {
            _cachedSpeakers = await _loadingTask;
            return _cachedSpeakers;
        }
        finally
        {
            _loadingTask = null;
        }
    }

    private async Task<List<string>> LoadSpeakersFromApiAsync()
    {
        try
        {
            var speakers = await _http.GetFromJsonAsync<List<string>>("api/meetings/speakers");
            return speakers ?? new List<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading speakers: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// Invalidates the cache, forcing a refresh on next request.
    /// Call this when a meeting is created/updated/deleted.
    /// </summary>
    public void InvalidateCache()
    {
        _cachedSpeakers = null;
    }
}
