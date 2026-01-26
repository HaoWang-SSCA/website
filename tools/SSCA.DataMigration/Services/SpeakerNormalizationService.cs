using System.Text.Json;
using Serilog;

namespace SSCA.DataMigration.Services;

/// <summary>
/// Service for normalizing speaker names to consolidate different spellings
/// </summary>
public class SpeakerNormalizationService
{
    private readonly Dictionary<string, string> _mappings;
    private readonly bool _trimWhitespace;
    private readonly bool _enabled;

    public SpeakerNormalizationService(string? mappingsFilePath = null, bool trimWhitespace = true)
    {
        _trimWhitespace = trimWhitespace;
        _mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        if (!string.IsNullOrEmpty(mappingsFilePath) && File.Exists(mappingsFilePath))
        {
            _enabled = LoadMappings(mappingsFilePath);
        }
        else
        {
            _enabled = false;
            Log.Information("No speaker mappings file found. Only basic normalization (trim whitespace) will be applied.");
        }
    }

    private bool LoadMappings(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("Mappings", out var mappingsElement))
            {
                foreach (var prop in mappingsElement.EnumerateObject())
                {
                    // Skip example entries
                    if (prop.Name.StartsWith("Example:")) continue;
                    
                    var sourceValue = prop.Name;
                    var targetValue = prop.Value.GetString();
                    
                    if (!string.IsNullOrEmpty(targetValue))
                    {
                        _mappings[sourceValue] = targetValue;
                    }
                }
            }

            Log.Information("Loaded {Count} speaker name mappings", _mappings.Count);
            return _mappings.Count > 0;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load speaker mappings from {File}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Normalize a speaker name using the mappings and basic normalization rules
    /// </summary>
    public string Normalize(string speakerName)
    {
        if (string.IsNullOrEmpty(speakerName))
            return speakerName;

        // Step 1: Basic normalization - trim whitespace
        var normalized = speakerName;
        if (_trimWhitespace)
        {
            normalized = normalized.Trim();
            // Also normalize internal whitespace (multiple spaces to single)
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");
        }

        // Step 2: Check exact mappings (case-insensitive)
        if (_mappings.TryGetValue(normalized, out var mapped))
        {
            Log.Debug("Speaker name mapped: '{Source}' -> '{Target}'", speakerName, mapped);
            return mapped;
        }

        // Step 3: Check if original (pre-trim) value has a mapping
        if (_mappings.TryGetValue(speakerName, out var mappedOriginal))
        {
            Log.Debug("Speaker name mapped: '{Source}' -> '{Target}'", speakerName, mappedOriginal);
            return mappedOriginal;
        }

        return normalized;
    }

    /// <summary>
    /// Get distinct speakers from source data for analysis
    /// </summary>
    public static void AnalyzeSpeakers(IEnumerable<string> speakers)
    {
        var speakerCounts = speakers
            .GroupBy(s => s?.Trim() ?? "")
            .OrderBy(g => g.Key)
            .ToList();

        Log.Information("=== Speaker Analysis ===");
        Log.Information("Found {Count} distinct speaker names:", speakerCounts.Count);
        
        foreach (var group in speakerCounts)
        {
            Log.Information("  '{Speaker}' - {Count} messages", group.Key, group.Count());
        }

        // Find potential duplicates (similar names)
        var potentialDuplicates = FindPotentialDuplicates(speakerCounts.Select(g => g.Key).ToList());
        if (potentialDuplicates.Any())
        {
            Log.Warning("=== Potential Duplicate Speakers (similar names) ===");
            foreach (var (name1, name2) in potentialDuplicates)
            {
                Log.Warning("  '{Name1}' might be same as '{Name2}'", name1, name2);
            }
        }
        
        Log.Information("========================");
    }

    private static List<(string, string)> FindPotentialDuplicates(List<string> speakers)
    {
        var duplicates = new List<(string, string)>();
        
        for (int i = 0; i < speakers.Count; i++)
        {
            for (int j = i + 1; j < speakers.Count; j++)
            {
                var name1 = speakers[i];
                var name2 = speakers[j];
                
                if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
                    continue;

                // Check for similarity
                if (IsSimilar(name1, name2))
                {
                    duplicates.Add((name1, name2));
                }
            }
        }

        return duplicates;
    }

    private static bool IsSimilar(string name1, string name2)
    {
        // Exact match after trimming
        if (name1.Trim() == name2.Trim())
            return true;

        // One contains the other (e.g., "王弟兄" vs "王弟兄牧师")
        if (name1.Contains(name2) || name2.Contains(name1))
            return true;

        // Similar length and same start (possible typo)
        if (Math.Abs(name1.Length - name2.Length) <= 2)
        {
            // Same first character and length difference <= 2
            if (name1.Length > 0 && name2.Length > 0 && name1[0] == name2[0])
            {
                // Calculate simple similarity
                var commonChars = name1.Intersect(name2).Count();
                var maxLen = Math.Max(name1.Length, name2.Length);
                if ((double)commonChars / maxLen > 0.7)
                    return true;
            }
        }

        return false;
    }

    public int MappingCount => _mappings.Count;
    public bool IsEnabled => _enabled || _trimWhitespace;
}
