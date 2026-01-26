using System.Net.Http.Json;

namespace SSCA.website.UI.Services;

/// <summary>
/// Service that caches speakers list on the client side to minimize API calls.
/// The cache persists for the lifetime of the application session.
/// </summary>
public class SpeakerService
{
    private readonly HttpClient _http;
    private Dictionary<string, List<string>> _cachedSpeakers = new();
    private Dictionary<string, Task<List<string>>> _loadingTasks = new();

    public SpeakerService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Gets the list of distinct speakers for a specific meeting type. Uses cached data if available.
    /// types: null (all), "sunday", "gospel", "special"
    /// </summary>
    public async Task<List<string>> GetSpeakersAsync(string? type = null)
    {
        var key = type ?? "all";

        // Return cached data if available
        if (_cachedSpeakers.TryGetValue(key, out var speakers))
        {
            return speakers;
        }

        // If already loading this type, wait for the existing task
        if (_loadingTasks.TryGetValue(key, out var task))
        {
            return await task;
        }

        // Start loading
        var loadingTask = LoadSpeakersFromApiAsync(type);
        _loadingTasks[key] = loadingTask;
        
        try
        {
            var result = await loadingTask;
            _cachedSpeakers[key] = result;
            return result;
        }
        finally
        {
            _loadingTasks.Remove(key);
        }
    }

    private async Task<List<string>> LoadSpeakersFromApiAsync(string? type)
    {
        try
        {
            var url = "api/meetings/speakers";
            if (!string.IsNullOrEmpty(type))
            {
                url += $"?type={type}";
            }
            var speakers = await _http.GetFromJsonAsync<List<string>>(url);
            return speakers ?? new List<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading speakers: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// Invalidates all speaker caches.
    /// </summary>
    public void InvalidateCache()
    {
        _cachedSpeakers.Clear();
    }
}
