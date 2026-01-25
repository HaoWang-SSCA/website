using System.Text.Json;
using SSCA.DataMigration.Models;
using Serilog;

namespace SSCA.DataMigration.Services;

/// <summary>
/// Manages migration progress persistence for resume capability
/// </summary>
public class ProgressService
{
    private readonly string _progressFilePath;
    private MigrationProgress _progress;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public ProgressService(string progressFilePath)
    {
        _progressFilePath = progressFilePath;
        _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true 
        };
        _progress = LoadProgress();
    }

    /// <summary>
    /// Load progress from file, or create new if not exists
    /// </summary>
    private MigrationProgress LoadProgress()
    {
        if (File.Exists(_progressFilePath))
        {
            try
            {
                var json = File.ReadAllText(_progressFilePath);
                var progress = JsonSerializer.Deserialize<MigrationProgress>(json, _jsonOptions);
                if (progress != null)
                {
                    Log.Information("Loaded existing progress file with {Count} records tracked", 
                        progress.Records.Count);
                    return progress;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load progress file, starting fresh");
            }
        }

        Log.Information("Starting new migration progress");
        return new MigrationProgress
        {
            StartedAt = DateTime.UtcNow,
            Status = MigrationStatus.NotStarted
        };
    }

    /// <summary>
    /// Save current progress to file
    /// </summary>
    public void SaveProgress()
    {
        lock (_lock)
        {
            try
            {
                _progress.LastUpdatedAt = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(_progress, _jsonOptions);
                File.WriteAllText(_progressFilePath, json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save progress file");
                throw;
            }
        }
    }

    /// <summary>
    /// Get the record key for a source record
    /// </summary>
    public static string GetRecordKey(string sourceTable, int sourceId)
    {
        return $"{sourceTable}_{sourceId}";
    }

    /// <summary>
    /// Check if a record has already been successfully migrated
    /// </summary>
    public bool IsRecordCompleted(string sourceTable, int sourceId)
    {
        var key = GetRecordKey(sourceTable, sourceId);
        if (_progress.Records.TryGetValue(key, out var record))
        {
            return record.Status == RecordStatus.Completed && 
                   record.DatabaseMigrated && 
                   record.AudioMigrated;
        }
        return false;
    }

    /// <summary>
    /// Check if a record's database migration is completed
    /// </summary>
    public bool IsDatabaseMigrated(string sourceTable, int sourceId)
    {
        var key = GetRecordKey(sourceTable, sourceId);
        if (_progress.Records.TryGetValue(key, out var record))
        {
            return record.DatabaseMigrated;
        }
        return false;
    }

    /// <summary>
    /// Check if a record's audio migration is completed
    /// </summary>
    public bool IsAudioMigrated(string sourceTable, int sourceId)
    {
        var key = GetRecordKey(sourceTable, sourceId);
        if (_progress.Records.TryGetValue(key, out var record))
        {
            return record.AudioMigrated;
        }
        return false;
    }

    /// <summary>
    /// Get or create record status
    /// </summary>
    public RecordMigrationStatus GetOrCreateRecordStatus(string sourceTable, int sourceId)
    {
        var key = GetRecordKey(sourceTable, sourceId);
        lock (_lock)
        {
            if (!_progress.Records.TryGetValue(key, out var record))
            {
                record = new RecordMigrationStatus
                {
                    SourceTable = sourceTable,
                    SourceId = sourceId,
                    Status = RecordStatus.Pending
                };
                _progress.Records[key] = record;
            }
            return record;
        }
    }

    /// <summary>
    /// Update a record's database migration status
    /// </summary>
    public void UpdateDatabaseMigration(string sourceTable, int sourceId, Guid targetId, bool success, string? error = null)
    {
        var key = GetRecordKey(sourceTable, sourceId);
        lock (_lock)
        {
            var record = GetOrCreateRecordStatus(sourceTable, sourceId);
            record.TargetId = targetId;
            record.DatabaseMigrated = success;
            record.LastProcessedAt = DateTime.UtcNow;
            
            if (!success)
            {
                record.ErrorMessage = error;
                record.RetryCount++;
            }

            UpdateRecordStatus(record);
            UpdateStatistics();
        }
    }

    /// <summary>
    /// Update a record's audio migration status
    /// </summary>
    public void UpdateAudioMigration(string sourceTable, int sourceId, string? sourceAudioFile, 
        string? targetBlobName, bool success, string? error = null)
    {
        var key = GetRecordKey(sourceTable, sourceId);
        lock (_lock)
        {
            var record = GetOrCreateRecordStatus(sourceTable, sourceId);
            record.SourceAudioFile = sourceAudioFile;
            record.TargetBlobName = targetBlobName;
            record.AudioMigrated = success;
            record.LastProcessedAt = DateTime.UtcNow;
            
            if (!success && !string.IsNullOrEmpty(error))
            {
                record.ErrorMessage = error;
                record.RetryCount++;
            }

            UpdateRecordStatus(record);
            UpdateStatistics();
        }
    }

    /// <summary>
    /// Mark a record as skipped (e.g., no audio file)
    /// </summary>
    public void SkipAudioMigration(string sourceTable, int sourceId, string reason)
    {
        var key = GetRecordKey(sourceTable, sourceId);
        lock (_lock)
        {
            var record = GetOrCreateRecordStatus(sourceTable, sourceId);
            record.AudioMigrated = true; // Mark as done (nothing to migrate)
            record.ErrorMessage = reason;
            record.LastProcessedAt = DateTime.UtcNow;
            
            UpdateRecordStatus(record);
            UpdateStatistics();
        }
    }

    private void UpdateRecordStatus(RecordMigrationStatus record)
    {
        if (record.DatabaseMigrated && record.AudioMigrated)
        {
            record.Status = RecordStatus.Completed;
        }
        else if (record.RetryCount > 0)
        {
            record.Status = RecordStatus.Failed;
        }
        else
        {
            record.Status = RecordStatus.InProgress;
        }
    }

    private void UpdateStatistics()
    {
        var stats = _progress.Statistics;
        
        var sundayRecords = _progress.Records.Values
            .Where(r => r.SourceTable == "sunday").ToList();
        var specialRecords = _progress.Records.Values
            .Where(r => r.SourceTable == "special").ToList();

        stats.MigratedSundayMessages = sundayRecords.Count(r => r.Status == RecordStatus.Completed);
        stats.MigratedSpecialMessages = specialRecords.Count(r => r.Status == RecordStatus.Completed);
        stats.FailedRecords = _progress.Records.Values.Count(r => r.Status == RecordStatus.Failed);
        stats.SkippedRecords = _progress.Records.Values.Count(r => r.Status == RecordStatus.Skipped);
        stats.AudioFilesUploaded = _progress.Records.Values.Count(r => 
            r.AudioMigrated && !string.IsNullOrEmpty(r.TargetBlobName));
    }

    /// <summary>
    /// Set total counts from source database
    /// </summary>
    public void SetTotalCounts(int sundayCount, int specialCount)
    {
        lock (_lock)
        {
            _progress.Statistics.TotalSundayMessages = sundayCount;
            _progress.Statistics.TotalSpecialMessages = specialCount;
        }
    }

    /// <summary>
    /// Get current migration progress
    /// </summary>
    public MigrationProgress GetProgress() => _progress;

    /// <summary>
    /// Update overall migration status
    /// </summary>
    public void SetStatus(MigrationStatus status)
    {
        lock (_lock)
        {
            _progress.Status = status;
        }
    }

    /// <summary>
    /// Print current progress summary
    /// </summary>
    public void PrintSummary()
    {
        var stats = _progress.Statistics;
        var total = stats.TotalSundayMessages + stats.TotalSpecialMessages;
        var completed = stats.MigratedSundayMessages + stats.MigratedSpecialMessages;
        
        Log.Information("=== Migration Progress Summary ===");
        Log.Information("Status: {Status}", _progress.Status);
        Log.Information("Sunday Messages: {Migrated}/{Total}", 
            stats.MigratedSundayMessages, stats.TotalSundayMessages);
        Log.Information("Special Messages: {Migrated}/{Total}", 
            stats.MigratedSpecialMessages, stats.TotalSpecialMessages);
        Log.Information("Total: {Completed}/{Total} ({Percent:P1})", 
            completed, total, total > 0 ? (double)completed / total : 0);
        Log.Information("Audio Files Uploaded: {Count}", stats.AudioFilesUploaded);
        Log.Information("Failed Records: {Count}", stats.FailedRecords);
        Log.Information("Skipped Records: {Count}", stats.SkippedRecords);
        Log.Information("================================");
    }

    /// <summary>
    /// Get records that need retry
    /// </summary>
    public IEnumerable<RecordMigrationStatus> GetRetryableRecords(int maxRetries)
    {
        return _progress.Records.Values
            .Where(r => r.Status == RecordStatus.Failed && r.RetryCount < maxRetries);
    }
}
