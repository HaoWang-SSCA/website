using SSCA.DataMigration.Models;
using Serilog;
using System.Globalization;

namespace SSCA.DataMigration.Services;

/// <summary>
/// Orchestrates the migration process with incremental sync and resume capability
/// </summary>
public class MigrationOrchestrator
{
    private readonly SourceDatabaseService _sourceDb;
    private readonly TargetDatabaseService _targetDb;
    private readonly AudioMigrationService _audioService;
    private readonly ProgressService _progressService;
    private readonly MigrationSettings _settings;

    public MigrationOrchestrator(
        SourceDatabaseService sourceDb,
        TargetDatabaseService targetDb,
        AudioMigrationService audioService,
        ProgressService progressService,
        MigrationSettings settings)
    {
        _sourceDb = sourceDb;
        _targetDb = targetDb;
        _audioService = audioService;
        _progressService = progressService;
        _settings = settings;
    }

    /// <summary>
    /// Run the full migration process
    /// </summary>
    public async Task<bool> RunMigrationAsync()
    {
        Log.Information("========================================");
        Log.Information("  SSCA Data Migration Tool");
        Log.Information("  Started at: {Time}", DateTime.Now);
        Log.Information("  Dry Run: {DryRun}", _settings.DryRun);
        Log.Information("========================================");

        try
        {
            // Step 1: Test all connections
            Log.Information("Step 1: Testing connections...");
            if (!await TestConnectionsAsync())
            {
                Log.Error("Connection test failed. Aborting migration.");
                return false;
            }

            // Step 2: Load source data
            Log.Information("Step 2: Loading source data...");
            var sundayMessages = await _sourceDb.GetSundayMessagesAsync();
            var specialMessages = await _sourceDb.GetSpecialMessagesAsync();

            _progressService.SetTotalCounts(sundayMessages.Count, specialMessages.Count);
            _progressService.SetStatus(MigrationStatus.InProgress);
            _progressService.SaveProgress();

            Log.Information("Found {Sunday} Sunday messages and {Special} Special messages",
                sundayMessages.Count, specialMessages.Count);

            // Step 3: Migrate Sunday messages
            Log.Information("Step 3: Migrating Sunday messages...");
            await MigrateSundayMessagesAsync(sundayMessages);

            // Step 4: Migrate Special messages
            Log.Information("Step 4: Migrating Special messages...");
            await MigrateSpecialMessagesAsync(specialMessages);

            // Step 5: Retry failed records
            Log.Information("Step 5: Retrying failed records...");
            await RetryFailedRecordsAsync();

            // Step 6: Final summary
            _progressService.SetStatus(
                _progressService.GetProgress().Statistics.FailedRecords > 0
                    ? MigrationStatus.CompletedWithErrors
                    : MigrationStatus.Completed);
            _progressService.SaveProgress();
            _progressService.PrintSummary();

            Log.Information("Migration completed at: {Time}", DateTime.Now);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Migration failed with exception");
            _progressService.SetStatus(MigrationStatus.Failed);
            _progressService.SaveProgress();
            return false;
        }
    }

    private async Task<bool> TestConnectionsAsync()
    {
        var sourceOk = await _sourceDb.TestConnectionAsync();
        var targetOk = await _targetDb.TestConnectionAsync();
        var storageOk = await _audioService.TestStorageConnectionAsync();
        var websiteOk = await _audioService.TestSourceWebsiteAsync();

        return sourceOk && targetOk && storageOk;
        // websiteOk is optional - audio files may be migrated separately
    }

    private async Task MigrateSundayMessagesAsync(List<SourceSundayMessage> messages)
    {
        var processed = 0;
        var skipped = 0;

        foreach (var source in messages)
        {
            processed++;
            
            // Check if already completed
            if (_progressService.IsRecordCompleted("sunday", source.Id))
            {
                skipped++;
                Log.Debug("Skipping completed record: sunday_{Id}", source.Id);
                continue;
            }

            Log.Information("Processing Sunday message {Current}/{Total}: ID={Id}, Date={Date}, Speaker={Speaker}",
                processed, messages.Count, source.Id, source.Date, source.Speaker);

            try
            {
                await MigrateSingleRecordAsync("sunday", source.Id, source.Date, source.DateTs,
                    source.Speaker, source.Theme, source.Gospel == 1, false, source.AudioFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error migrating Sunday message {Id}", source.Id);
                _progressService.UpdateDatabaseMigration("sunday", source.Id, Guid.Empty, false, ex.Message);
            }

            // Save progress periodically
            if (processed % _settings.BatchSize == 0)
            {
                _progressService.SaveProgress();
            }
        }

        _progressService.SaveProgress();
        Log.Information("Sunday messages: Processed {Processed}, Skipped {Skipped}", processed, skipped);
    }

    private async Task MigrateSpecialMessagesAsync(List<SourceSpecialMessage> messages)
    {
        var processed = 0;
        var skipped = 0;

        foreach (var source in messages)
        {
            processed++;
            
            // Check if already completed
            if (_progressService.IsRecordCompleted("special", source.Id))
            {
                skipped++;
                Log.Debug("Skipping completed record: special_{Id}", source.Id);
                continue;
            }

            Log.Information("Processing Special message {Current}/{Total}: ID={Id}, Date={Date}, Speaker={Speaker}",
                processed, messages.Count, source.Id, source.Date, source.Speaker);

            try
            {
                await MigrateSingleRecordAsync("special", source.Id, source.Date, source.DateTs,
                    source.Speaker, source.Theme, source.Gospel == 1, true, source.AudioFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error migrating Special message {Id}", source.Id);
                _progressService.UpdateDatabaseMigration("special", source.Id, Guid.Empty, false, ex.Message);
            }

            // Save progress periodically
            if (processed % _settings.BatchSize == 0)
            {
                _progressService.SaveProgress();
            }
        }

        _progressService.SaveProgress();
        Log.Information("Special messages: Processed {Processed}, Skipped {Skipped}", processed, skipped);
    }

    private async Task MigrateSingleRecordAsync(
        string sourceTable,
        int sourceId,
        string dateStr,
        int dateTs,
        string speaker,
        string topic,
        bool isGospel,
        bool isSpecialMeeting,
        string audioFile)
    {
        // Parse date - try multiple formats
        var date = ParseDate(dateStr, dateTs);
        
        // Prepare target blob name
        var blobName = !string.IsNullOrEmpty(audioFile) 
            ? _audioService.ComposeBlobName(sourceTable, audioFile, date) 
            : null;

        Guid targetId;

        // Step A: Migrate database record (if not already done)
        if (!_progressService.IsDatabaseMigrated(sourceTable, sourceId))
        {
            if (_settings.DryRun)
            {
                targetId = Guid.NewGuid();
                Log.Information("[DRY RUN] Would insert: Date={Date}, Speaker={Speaker}, Topic={Topic}",
                    date, speaker, topic);
            }
            else
            {
                // Check if already exists in target
                var existingId = await _targetDb.FindExistingRecordAsync(date, speaker, topic, isGospel, isSpecialMeeting);
                
                if (existingId.HasValue)
                {
                    targetId = existingId.Value;
                    Log.Debug("Record already exists in target: {Id}", targetId);
                }
                else
                {
                    // Create new record
                    var meeting = new TargetMessageMeeting
                    {
                        Id = Guid.NewGuid(),
                        Date = date,
                        Speaker = speaker,
                        Topic = topic,
                        AudioBlobName = blobName,
                        VideoUrl = null,
                        IsGospel = isGospel,
                        IsSpecialMeeting = isSpecialMeeting,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    targetId = await _targetDb.InsertMessageMeetingAsync(meeting);
                    Log.Information("Inserted database record: {Id}", targetId);
                }
            }

            _progressService.UpdateDatabaseMigration(sourceTable, sourceId, targetId, true);
        }
        else
        {
            // Get existing target ID from progress
            var recordStatus = _progressService.GetOrCreateRecordStatus(sourceTable, sourceId);
            targetId = recordStatus.TargetId ?? Guid.Empty;
        }

        // Step B: Migrate audio file (if not already done)
        if (!_progressService.IsAudioMigrated(sourceTable, sourceId))
        {
            if (string.IsNullOrEmpty(audioFile))
            {
                _progressService.SkipAudioMigration(sourceTable, sourceId, "No audio file");
                Log.Debug("No audio file for record {Table}_{Id}", sourceTable, sourceId);
            }
            else
            {
                var sourceUrl = sourceTable == "sunday"
                    ? _audioService.ComposeSundayAudioUrl(audioFile)
                    : _audioService.ComposeSpecialAudioUrl(audioFile);

                if (_settings.DryRun)
                {
                    Log.Information("[DRY RUN] Would upload: {Source} -> {Target}", sourceUrl, blobName);
                    _progressService.UpdateAudioMigration(sourceTable, sourceId, audioFile, blobName, true);
                }
                else
                {
                    var (success, bytes, error) = await _audioService.MigrateAudioFileAsync(sourceUrl, blobName!);
                    _progressService.UpdateAudioMigration(sourceTable, sourceId, audioFile, blobName, success, error);
                    
                    // Update database with blob name if needed
                    if (success && targetId != Guid.Empty)
                    {
                        await _targetDb.UpdateAudioBlobNameAsync(targetId, blobName!);
                    }
                }
            }
        }
    }

    private async Task RetryFailedRecordsAsync()
    {
        var retryableRecords = _progressService.GetRetryableRecords(_settings.RetryCount).ToList();
        
        if (!retryableRecords.Any())
        {
            Log.Information("No failed records to retry");
            return;
        }

        Log.Information("Retrying {Count} failed records...", retryableRecords.Count);

        foreach (var record in retryableRecords)
        {
            Log.Information("Retrying: {Table}_{Id} (Attempt {Attempt}/{Max})",
                record.SourceTable, record.SourceId, record.RetryCount + 1, _settings.RetryCount);

            await Task.Delay(TimeSpan.FromSeconds(_settings.RetryDelaySeconds));

            // Re-fetch the source record and retry
            // For now, just retry the audio migration if that failed
            if (!record.AudioMigrated && !string.IsNullOrEmpty(record.SourceAudioFile))
            {
                var sourceUrl = record.SourceTable == "sunday"
                    ? _audioService.ComposeSundayAudioUrl(record.SourceAudioFile)
                    : _audioService.ComposeSpecialAudioUrl(record.SourceAudioFile);

                var (success, bytes, error) = await _audioService.MigrateAudioFileAsync(
                    sourceUrl, record.TargetBlobName!);
                    
                _progressService.UpdateAudioMigration(
                    record.SourceTable, record.SourceId, 
                    record.SourceAudioFile, record.TargetBlobName, 
                    success, error);

                if (success && record.TargetId.HasValue)
                {
                    await _targetDb.UpdateAudioBlobNameAsync(record.TargetId.Value, record.TargetBlobName!);
                }
            }
        }

        _progressService.SaveProgress();
    }

    /// <summary>
    /// Parse date from source format - supports multiple formats
    /// </summary>
    private static DateTime ParseDate(string dateStr, int dateTs)
    {
        // Try parsing the string format first
        if (!string.IsNullOrEmpty(dateStr))
        {
            // Try common formats
            string[] formats = { 
                "yyyy-MM-dd", 
                "yyyy/MM/dd", 
                "MM/dd/yyyy",
                "dd/MM/yyyy",
                "yyyy_MM_dd"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateStr.Trim(), format, 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                }
            }

            // Try generic parse
            if (DateTime.TryParse(dateStr, out var genericParsed))
            {
                return DateTime.SpecifyKind(genericParsed, DateTimeKind.Utc);
            }
        }

        // Fall back to Unix timestamp
        if (dateTs > 0)
        {
            return DateTimeOffset.FromUnixTimeSeconds(dateTs).UtcDateTime;
        }

        // Last resort: use current date
        Log.Warning("Could not parse date: '{DateStr}', timestamp: {DateTs}. Using current date.",
            dateStr, dateTs);
        return DateTime.UtcNow;
    }
}

/// <summary>
/// Migration settings from configuration
/// </summary>
public class MigrationSettings
{
    public int BatchSize { get; set; } = 10;
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public bool DryRun { get; set; } = false;
}
