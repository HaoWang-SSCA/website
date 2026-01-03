# Data Migration Strategy: MySQL + Local File Storage → Azure PostgreSQL + Azure Blob Storage

## Overview

This document outlines a comprehensive approach to migrate your SSCA website data from:
- **Source**: MySQL database + Server folder for audio files
- **Target**: Azure PostgreSQL + Azure Blob Storage

The strategy is designed to be executed with **minimal downtime** and includes validation checkpoints to ensure data integrity.

---

## Phase 1: Pre-Migration Preparation

### 1.1 Inventory & Assessment

**Tasks:**
- [ ] Export MySQL database schema from old site
- [ ] Identify all database tables and relationships
- [ ] List all audio files in the server folder (count, size, formats)
- [ ] Document any custom database extensions or triggers
- [ ] Estimate total data volume (database size + audio file size)
- [ ] Create backup of MySQL database and audio files

**Deliverables:**
- Backup files stored securely
- Data inventory spreadsheet (table names, row counts, audio file counts)
- Schema comparison document (MySQL vs PostgreSQL differences)

### 1.2 Map Old Schema to New Schema

**Current New System Model** (from `MessageMeeting.cs`):
```csharp
public class MessageMeeting
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Speaker { get; set; }           // 讲员
    public string Topic { get; set; }             // 信息主题
    public string? AudioBlobName { get; set; }    // Azure Blob reference
    public string? VideoUrl { get; set; }
    public bool IsGospel { get; set; }
    public bool IsSpecialMeeting { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Mapping Tasks:**
- [ ] Map old database columns to new `MessageMeeting` fields
- [ ] Identify how old system stored audio file references (file path? file ID?)
- [ ] Determine migration of Gospel vs Sunday vs Special meeting flags
- [ ] Plan for CreatedAt/UpdatedAt timestamps (use migration date if unavailable)

---

## Phase 2: Data Migration Execution

### 2.1 Database Migration

#### Option A: SQL-Based Export/Import (Recommended for Control)

**Steps:**

1. **Export MySQL data as SQL/CSV:**
   ```bash
   # Export from old MySQL database
   mysqldump -u [user] -p [database] > backup.sql
   
   # Or export specific table as CSV (for review)
   SELECT * FROM meetings 
   INTO OUTFILE '/tmp/meetings.csv' 
   FIELDS TERMINATED BY ',' 
   ENCLOSED BY '"';
   ```

2. **Transform data** (using a migration script):
   - Create a C# console application or PowerShell script
   - Read CSV/JSON from MySQL export
   - Transform old schema → new schema
   - Handle NULL values, date formats, string lengths
   - Generate INSERT statements for PostgreSQL

   **Sample C# Migration Script Structure:**
   ```csharp
   // Read old data
   var oldMeetings = ReadFromMysqlExport("meetings.csv");
   
   // Transform to new schema
   var newMeetings = oldMeetings.Select(m => new MessageMeeting
   {
       Id = Guid.NewGuid(),  // Generate new IDs
       Date = m.MeetingDate,
       Speaker = m.SpeakerName,
       Topic = m.MessageTitle,
       AudioBlobName = $"legacy/{m.AudioFileName}",  // Mark as legacy
       VideoUrl = m.VideoLink,
       IsGospel = m.Type == "Gospel",
       IsSpecialMeeting = m.Type == "Special",
       CreatedAt = m.CreatedDate ?? DateTime.Now,
       UpdatedAt = m.UpdatedDate ?? DateTime.Now
   }).ToList();
   
   // Validate
   ValidateData(newMeetings);
   
   // Insert to PostgreSQL
   await InsertToAzurePostgreSQL(newMeetings);
   ```

3. **Validate inserted data:**
   ```sql
   -- Run against new PostgreSQL database
   SELECT COUNT(*) FROM public."MessageMeetings";
   SELECT COUNT(*) FROM public."MessageMeetings" WHERE "AudioBlobName" IS NOT NULL;
   SELECT MIN("Date"), MAX("Date") FROM public."MessageMeetings";
   ```

#### Option B: Entity Framework Code-First (Simplest for New System)

**Steps:**

1. Create a migration helper class to read old MySQL data
2. Use EF Core to insert into PostgreSQL directly
3. Build into your existing application migration pattern

```csharp
// In Program.cs or migration service
public async Task MigrateFromLegacyDatabase()
{
    using var mysqlConnection = new MySqlConnection(legacyConnectionString);
    
    var oldMeetings = await ReadOldMeetingsAsync(mysqlConnection);
    var newMeetings = TransformData(oldMeetings);
    
    using var dbContext = new AppDbContext(options);
    await dbContext.MessageMeetings.AddRangeAsync(newMeetings);
    await dbContext.SaveChangesAsync();
}
```

### 2.2 Audio File Migration

#### Step 1: Inventory Audio Files
```powershell
# PowerShell to inventory files on old server
$audioPath = "C:\path\to\audio\folder"
$inventory = Get-ChildItem -Path $audioPath -Recurse -File | 
    Select-Object FullName, Length, LastWriteTime, Extension

# Export to CSV for tracking
$inventory | Export-Csv -Path "audio_inventory.csv" -NoTypeInformation

# Get total size
$totalSize = (Get-ChildItem -Path $audioPath -Recurse -File | 
    Measure-Object -Property Length -Sum).Sum
Write-Host "Total audio files: $($inventory.Count)"
Write-Host "Total size: $($totalSize / 1GB) GB"
```

#### Step 2: Upload to Azure Blob Storage

**Option A: Azure CLI (Simple, Manual)**
```bash
# Install Azure CLI if not present
# Login to Azure
az login

# Create container if not exists
az storage container create \
  --name audio \
  --account-name <storage-account-name>

# Upload all audio files
# Using azcopy (faster for large volumes)
azcopy copy "C:\path\to\audio\folder" "https://<account>.blob.core.windows.net/audio" --recursive

# Verify uploads
azcopy list "https://<account>.blob.core.windows.net/audio" --recursive
```

**Option B: C# Console App (Automated, Flexible)**

```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public class AudioMigrationService
{
    private readonly BlobContainerClient _containerClient;
    
    public AudioMigrationService(string connectionString)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient("audio");
    }
    
    public async Task MigrateAudioFilesAsync(string localAudioPath)
    {
        var audioFiles = Directory.GetFiles(localAudioPath, "*.*", SearchOption.AllDirectories);
        var totalFiles = audioFiles.Length;
        
        for (int i = 0; i < audioFiles.Length; i++)
        {
            var file = audioFiles[i];
            var relativePath = Path.GetRelativePath(localAudioPath, file);
            var blobName = $"legacy/{relativePath}".Replace("\\", "/");
            
            Console.WriteLine($"Uploading {i + 1}/{totalFiles}: {blobName}");
            
            // Upload file
            var blobClient = _containerClient.GetBlobClient(blobName);
            await using var fileStream = File.OpenRead(file);
            await blobClient.UploadAsync(fileStream, overwrite: true);
            
            // Verify file exists
            if (await blobClient.ExistsAsync())
            {
                Console.WriteLine($"✓ Success: {blobName}");
            }
        }
    }
}
```

#### Step 3: Generate Blob Name Mappings

Create a CSV file mapping old file paths to new blob names for database update:

```csv
OldFileName,NewBlobName,Status
audio/message/2024/sunday_20240101.mp3,legacy/message/2024/sunday_20240101.mp3,uploaded
audio/gospel/easter_2024.mp3,legacy/gospel/easter_2024.mp3,uploaded
```

#### Step 4: Update Database References

Once all files are in Azure Storage, update the `AudioBlobName` field in the database:

```sql
-- Update existing messages with audio file references
-- Using the mapping from Step 3
UPDATE public."MessageMeetings" m
SET "AudioBlobName" = mapping."NewBlobName"
FROM legacy_audio_mapping mapping
WHERE m.Speaker = mapping.OldSpeaker 
  AND m."Date" = mapping.OldDate;
```

---

## Phase 3: Connection String & Configuration

### 3.1 Update Application Configuration

**In `Program.cs` or configuration files:**

```csharp
// PostgreSQL connection
var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL");
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection)
);

// Azure Storage connection
var storageConnection = builder.Configuration.GetConnectionString("AzureStorage");
services.AddSingleton(x => new BlobServiceClient(storageConnection));
```

**In `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Server=<server>.postgres.database.azure.com;Database=ssca_website;User Id=<username>;Password=<password>;Ssl Mode=Require;",
    "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net"
  }
}
```

### 3.2 Deploy Updated Application

```bash
cd c:\Projects\HaoWang-SSCA\website\src\SSCA.website.API

# Build and test
dotnet build

# Deploy to Azure Static Web Apps or your hosting environment
# (Update your deployment process)
```

---

## Phase 4: Validation & Testing

### 4.1 Data Integrity Checks

**Run these queries against new PostgreSQL database:**

```sql
-- Check row counts match
SELECT 'MessageMeetings' as table_name, COUNT(*) as count FROM public."MessageMeetings";

-- Check for orphaned audio references
SELECT * FROM public."MessageMeetings" 
WHERE "AudioBlobName" IS NOT NULL 
  AND "AudioBlobName" NOT LIKE 'legacy/%';

-- Check date range
SELECT 
    MIN("Date") as earliest_message,
    MAX("Date") as latest_message,
    COUNT(*) as total_messages
FROM public."MessageMeetings";

-- Check audio file distribution
SELECT 
    COUNT(*) as total_messages,
    COUNT(CASE WHEN "AudioBlobName" IS NOT NULL THEN 1 END) as with_audio,
    COUNT(CASE WHEN "VideoUrl" IS NOT NULL THEN 1 END) as with_video
FROM public."MessageMeetings";
```

### 4.2 Audio File Verification

```powershell
# Verify all blob files are accessible
$blobUri = "https://<account>.blob.core.windows.net/audio"

# Test access to a few random files
# (Requires azcopy or Azure CLI)
azcopy list "$blobUri/legacy" --recursive | head -20
```

### 4.3 Application Testing

**In development environment:**
1. Build and run the application against new PostgreSQL database
2. Test message list display (verify all data loads)
3. Test audio file playback (verify blob links are valid)
4. Test admin CRUD operations
5. Verify search/filter functionality

**Specific test cases:**
- [ ] Load all messages and verify count matches MySQL
- [ ] Play audio file from migration (verify URL generation works)
- [ ] Edit a message and verify data persists correctly
- [ ] Create new message in new system
- [ ] Verify permissions/authentication works in new system

---

## Phase 5: Cutover & Going Live

### 5.1 Final Pre-Production Checklist

- [ ] All backups completed and stored securely
- [ ] Dry-run migration executed and validated
- [ ] Application tested against new database in staging environment
- [ ] Rollback plan documented and tested
- [ ] Maintenance window scheduled and communicated
- [ ] Database indexes created on frequently queried fields (`Date`, `IsGospel`, etc.)
- [ ] Connection strings updated in all environments
- [ ] Monitoring/logging configured for new database

### 5.2 Production Migration Steps

1. **Backup everything** (old MySQL, old audio folder)
   ```bash
   mysqldump -u root -p ssca_db > /backups/ssca_db_final_backup.sql
   cp -r /var/www/audio /backups/audio_final_backup
   ```

2. **Execute migration** (run all phases 2-4)

3. **Update DNS/routing** to point to new Azure infrastructure

4. **Monitor for 24 hours:**
   - Check application logs for errors
   - Verify users can access all content
   - Monitor audio playback issues

5. **Disable old system** (keep for 30 days as cold backup)

### 5.3 Rollback Plan

If critical issues occur:

1. Restore old system to production
2. Identify root cause of issue
3. Fix data migration logic
4. Schedule retry migration

---

## Phase 6: Cleanup & Optimization

### 6.1 Database Optimization

```sql
-- Create indexes for frequently accessed columns
CREATE INDEX idx_message_date ON public."MessageMeetings"("Date" DESC);
CREATE INDEX idx_message_gospel ON public."MessageMeetings"("IsGospel") 
    WHERE "IsGospel" = true;
CREATE INDEX idx_message_special ON public."MessageMeetings"("IsSpecialMeeting") 
    WHERE "IsSpecialMeeting" = true;

-- Analyze tables for query optimization
ANALYZE public."MessageMeetings";
```

### 6.2 Archive Old Data

- Keep MySQL backup for 90 days minimum
- Delete old audio files from server after confirming all are in blob storage
- Document any legacy data that couldn't be migrated

### 6.3 Update Documentation

- [ ] Update DEVELOPMENT_SETUP.md with PostgreSQL connection details
- [ ] Document blob storage naming conventions
- [ ] Update API documentation with new audio URL format
- [ ] Update deployment guides

---

## Tools & Technologies Summary

| Task | Tool | Notes |
|------|------|-------|
| MySQL Export | `mysqldump`, MySQL Workbench | Include schema and data |
| Data Transformation | C# Console App, PowerShell | Custom script for schema mapping |
| Audio Upload | Azure CLI (`azcopy`) or C# SDK | Use azcopy for large volumes |
| Database Migration | Entity Framework Core, raw SQL | EF for new inserts, SQL for validation |
| Verification | Azure CLI, SQL queries | Run against both databases for comparison |
| Monitoring | Azure Monitor, Application Insights | Monitor new PostgreSQL and storage |

---

## Timeline Estimate

| Phase | Duration | Notes |
|-------|----------|-------|
| Pre-Migration | 1-2 days | Assessment and planning |
| Database Migration | 1-4 hours | Depends on data volume |
| Audio Migration | 2-8 hours | Depends on total audio file size |
| Validation | 2-4 hours | Run all test cases |
| Cutover | 30 mins - 2 hours | DNS update + monitoring |
| Cleanup | 1 day | Optimization and documentation |
| **Total** | **2-4 weeks** | Including testing and buffer time |

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Data corruption during transfer | Validate checksums; compare row counts before/after |
| Lost audio files | Maintain offline backup; verify upload completion before deleting source |
| Downtime during migration | Use staging environment for dry-run; execute migration during off-hours |
| Audio URL breakage | Test blob URLs before going live; update URL generation logic if needed |
| Performance degradation | Create indexes; test query performance against new database |
| Authentication/authorization issues | Test permissions thoroughly; maintain admin access during migration |

---

## Next Steps

1. **Assign ownership**: Who will handle each phase?
2. **Create detailed data mapping**: Map old MySQL schema to new C# models
3. **Develop migration scripts**: Build C# console app or PowerShell scripts
4. **Execute dry-run**: Test entire process in non-production environment
5. **Schedule production cutover**: Coordinate with stakeholders

