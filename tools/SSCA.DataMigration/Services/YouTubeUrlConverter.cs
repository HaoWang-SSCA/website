namespace SSCA.DataMigration.Services;

/// <summary>
/// Utility class to convert YouTube URLs to embed format
/// </summary>
public static class YouTubeUrlConverter
{
    /// <summary>
    /// Convert any YouTube URL format to the embed URL format
    /// </summary>
    /// <param name="url">Original YouTube URL (watch, youtu.be, live, etc.)</param>
    /// <returns>YouTube embed URL or null if not a valid YouTube URL</returns>
    public static string? ConvertToEmbedUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var videoId = ExtractVideoId(url);
        
        if (string.IsNullOrEmpty(videoId))
            return null;

        // Use youtube-nocookie.com for enhanced privacy
        return $"https://www.youtube-nocookie.com/embed/{videoId}";
    }

    /// <summary>
    /// Extract the video ID from various YouTube URL formats
    /// </summary>
    public static string? ExtractVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        try
        {
            // Format: https://www.youtube.com/watch?v=VIDEO_ID
            // Format: https://www.youtube.com/watch?v=VIDEO_ID&list=...
            if (url.Contains("watch?v="))
            {
                var parts = url.Split("watch?v=");
                if (parts.Length > 1)
                {
                    return parts[1].Split('&')[0].Split('?')[0];
                }
            }

            // Format: https://youtu.be/VIDEO_ID
            // Format: https://youtu.be/VIDEO_ID?si=...
            if (url.Contains("youtu.be/"))
            {
                var parts = url.Split("youtu.be/");
                if (parts.Length > 1)
                {
                    return parts[1].Split('?')[0].Split('&')[0];
                }
            }

            // Format: https://youtube.com/live/VIDEO_ID
            // Format: https://www.youtube.com/live/VIDEO_ID?si=...
            if (url.Contains("/live/"))
            {
                var parts = url.Split("/live/");
                if (parts.Length > 1)
                {
                    return parts[1].Split('?')[0].Split('&')[0];
                }
            }

            // Already an embed URL: https://www.youtube.com/embed/VIDEO_ID
            if (url.Contains("/embed/"))
            {
                var parts = url.Split("/embed/");
                if (parts.Length > 1)
                {
                    return parts[1].Split('?')[0].Split('&')[0];
                }
            }

            // Old format: https://www.youtube.com/v/VIDEO_ID
            if (url.Contains("/v/"))
            {
                var parts = url.Split("/v/");
                if (parts.Length > 1)
                {
                    return parts[1].Split('?')[0].Split('&')[0];
                }
            }
        }
        catch
        {
            // If parsing fails, return null
        }

        return null;
    }

    /// <summary>
    /// Check if a URL is a valid YouTube URL
    /// </summary>
    public static bool IsYouTubeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return url.Contains("youtube.com") || url.Contains("youtu.be");
    }
}
