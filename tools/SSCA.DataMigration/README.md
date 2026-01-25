# SSCA Data Migration Tool

A C# console application for migrating data from the legacy PHP website (MySQL + file storage) to the new Blazor website (PostgreSQL + Azure Blob Storage).

## Features

- **Incremental Sync**: Migrates records one at a time, tracking progress for each
- **Resume Capability**: If interrupted, re-running will continue from where it left off
- **Transaction-based**: Each record + its audio file is treated as a single unit
- **Configurable**: All connection strings and settings are in `appsettings.json`
- **Dry Run Mode**: Preview changes without actually modifying databases
- **Detailed Logging**: Console and file logging with timestamps

## Prerequisites

- .NET 8.0 SDK
- Access to source MySQL database
- Access to target PostgreSQL database
- Azure Storage account with Blob Storage configured
- Network access to the source website (for audio file download)

## Configuration

Edit `appsettings.json` before running:

```json
{
  "SourceDatabase": {
    "ConnectionString": "Server=YOUR_MYSQL_HOST;Port=3306;Database=sscadb20140309;User=YOUR_USER;Password=YOUR_PASSWORD;CharSet=utf8;"
  },
  "TargetDatabase": {
    "ConnectionString": "Host=YOUR_PG_HOST;Port=5432;Database=ssca_website;Username=YOUR_USER;Password=YOUR_PASSWORD;"
  },
  "SourceWebsite": {
    "Domain": "https://www.ssca-bc.org",
    "SundayMessageAudioPath": "/messages/sundaymsg",
    "SpecialMessageAudioPath": "/messages/specialmsg"
  },
  "TargetStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net",
    "ContainerName": "audio-files"
  },
  "Migration": {
    "ProgressFile": "migration_progress.json",
    "BatchSize": 10,
    "RetryCount": 3,
    "RetryDelaySeconds": 5,
    "DryRun": false
  }
}
```

## Usage

### Build

```bash
cd tools/SSCA.DataMigration
dotnet build
```

### Run (Dry Run First)

```bash
# Test without making changes
dotnet run -- --Migration:DryRun=true
```

### Run (Actual Migration)

```bash
dotnet run
```

### Command Line Overrides

You can override any setting via command line:

```bash
# Override specific settings
dotnet run -- --SourceDatabase:ConnectionString="Server=...;..." --Migration:DryRun=true
```

## How It Works

### Migration Process

1. **Connection Test**: Verifies connectivity to MySQL, PostgreSQL, and Azure Storage
2. **Load Source Data**: Reads all records from `ssca_sunday_msg` and `ssca_special_msg` tables
3. **Migrate Sunday Messages**: For each record:
   - Check if already completed (skip if yes)
   - Insert/update database record in PostgreSQL
   - Download audio file from source website
   - Upload audio file to Azure Blob Storage
   - Update progress file
4. **Migrate Special Messages**: Same process as Sunday messages
5. **Retry Failed Records**: Attempts to retry any failed migrations
6. **Final Summary**: Reports statistics

### Schema Mapping

The source MySQL database stores meetings in two tables (`ssca_sunday_msg` and `ssca_special_msg`), with a `gospel` flag to indicate gospel messages. The target uses a single unified table with flags.

**Source → Target Mapping:**

| Source Table | Source `gospel` Flag | Target `IsGospel` | Target `IsSpecialMeeting` | UI Page |
|-------------|---------------------|-------------------|--------------------------|---------|
| `ssca_sunday_msg` | 0 (false) | `false` | `false` | Sunday Messages |
| `ssca_sunday_msg` | 1 (true) | `true` | `false` | **Gospel Meetings** |
| `ssca_special_msg` | 0 or 1 | based on source | `true` | Special Meetings |

**Field Mapping (ssca_sunday_msg → MessageMeetings):**

| Source (MySQL)             | Target (PostgreSQL)      |
|---------------------------|--------------------------|
| `id`                      | `Id` (new GUID generated) |
| `date` / `date_ts`        | `Date` (parsed to DateTime) |
| `speaker`                 | `Speaker` |
| `theme`                   | `Topic` |
| `gospel`                  | `IsGospel` (1 → true, 0 → false) |
| -                         | `IsSpecialMeeting = false` |
| `audio_file`              | `AudioBlobName` (Azure Blob path) |

**Field Mapping (ssca_special_msg → MessageMeetings):**

| Source (MySQL)             | Target (PostgreSQL)      |
|---------------------------|--------------------------|
| `id`                      | `Id` (new GUID generated) |
| `date` / `date_ts`        | `Date` (parsed to DateTime) |
| `speaker`                 | `Speaker` |
| `theme`                   | `Topic` |
| `gospel`                  | `IsGospel` (1 → true, 0 → false) |
| -                         | `IsSpecialMeeting = true` |
| `audio_file`              | `AudioBlobName` (Azure Blob path) |

### Audio File Organization

Audio files are organized in Azure Blob Storage as:
```
audio-files/
├── sunday/
│   ├── 2018/
│   │   ├── 2018_07_29.mp3
│   │   └── 2018_08_12.mp3
│   └── 2024/
│       └── ...
└── special/
    ├── 2018/
    └── ...
```

### Progress File (`migration_progress.json`)

The progress file tracks the status of each record:

```json
{
  "StartedAt": "2024-01-15T10:00:00Z",
  "LastUpdatedAt": "2024-01-15T10:30:00Z",
  "Status": "InProgress",
  "Records": {
    "sunday_1": {
      "SourceTable": "sunday",
      "SourceId": 1,
      "TargetId": "abc-123-...",
      "Status": "Completed",
      "DatabaseMigrated": true,
      "AudioMigrated": true,
      "SourceAudioFile": "2018_07_29.mp3",
      "TargetBlobName": "sunday/2018/2018_07_29.mp3"
    }
  },
  "Statistics": {
    "TotalSundayMessages": 100,
    "MigratedSundayMessages": 50,
    "FailedRecords": 2
  }
}
```

## Resume After Interruption

Simply run the tool again. It will:
1. Load the existing progress file
2. Skip any records marked as completed
3. Continue with pending records
4. Retry failed records (up to configured retry count)

## Troubleshooting

### Connection Errors

- **MySQL**: Ensure the server allows remote connections and the user has SELECT permissions
- **PostgreSQL**: Check connection string format and network access
- **Azure Storage**: Verify account name and key are correct

### Audio Download Failures

- Check if the source website is accessible
- Verify the audio file paths in configuration match the actual URLs
- Some audio files may not exist - these are logged and skipped

### View Logs

Logs are saved to the `logs/` directory with timestamp-based filenames.

## Reset Migration

To start fresh, delete the progress file:

```bash
rm migration_progress.json
```

⚠️ **Warning**: This will cause all records to be re-processed. Database records with matching data will be skipped, but audio files may be re-uploaded.
