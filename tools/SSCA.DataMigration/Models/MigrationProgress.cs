using System.Text.Json.Serialization;

namespace SSCA.DataMigration.Models;

/// <summary>
/// Tracks migration progress to enable incremental sync and resume capability
/// </summary>
public class MigrationProgress
{
    /// <summary>
    /// When the migration was started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the migration was last updated
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Overall migration status
    /// </summary>
    public MigrationStatus Status { get; set; } = MigrationStatus.NotStarted;

    /// <summary>
    /// Track each record's migration status
    /// Key format: "{source_table}_{source_id}", e.g., "sunday_123" or "special_456"
    /// </summary>
    public Dictionary<string, RecordMigrationStatus> Records { get; set; } = new();

    /// <summary>
    /// Statistics for reporting
    /// </summary>
    public MigrationStatistics Statistics { get; set; } = new();
}

public class RecordMigrationStatus
{
    /// <summary>
    /// Source table (sunday or special)
    /// </summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>
    /// Source record ID in MySQL
    /// </summary>
    public int SourceId { get; set; }

    /// <summary>
    /// Target record ID in PostgreSQL (Guid)
    /// </summary>
    public Guid? TargetId { get; set; }

    /// <summary>
    /// Current status of this record's migration
    /// </summary>
    public RecordStatus Status { get; set; } = RecordStatus.Pending;

    /// <summary>
    /// Whether the database record has been migrated
    /// </summary>
    public bool DatabaseMigrated { get; set; }

    /// <summary>
    /// Whether the audio file has been migrated
    /// </summary>
    public bool AudioMigrated { get; set; }

    /// <summary>
    /// Source audio filename
    /// </summary>
    public string? SourceAudioFile { get; set; }

    /// <summary>
    /// Target blob name in Azure Storage
    /// </summary>
    public string? TargetBlobName { get; set; }

    /// <summary>
    /// Error message if migration failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// When this record was last processed
    /// </summary>
    public DateTime LastProcessedAt { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MigrationStatus
{
    NotStarted,
    InProgress,
    Completed,
    CompletedWithErrors,
    Failed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RecordStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped
}

public class MigrationStatistics
{
    public int TotalSundayMessages { get; set; }
    public int TotalSpecialMessages { get; set; }
    public int MigratedSundayMessages { get; set; }
    public int MigratedSpecialMessages { get; set; }
    public int FailedRecords { get; set; }
    public int SkippedRecords { get; set; }
    public int AudioFilesUploaded { get; set; }
    public int AudioFilesFailed { get; set; }
    public long TotalAudioBytesUploaded { get; set; }
}
