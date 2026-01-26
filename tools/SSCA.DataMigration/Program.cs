using Microsoft.Extensions.Configuration;
using Serilog;
using SSCA.DataMigration.Services;

namespace SSCA.DataMigration;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Build configuration
        // appsettings.json = template with placeholders (committed to git)
        // appsettings.local.json = actual secrets (gitignored)
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true) // Overrides with secrets
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        // Configure logging
        var logDirectory = configuration["Logging:LogDirectory"] ?? "logs";
        var logFileName = configuration["Logging:LogFileName"] ?? "migration_{Date}.log";
        
        Directory.CreateDirectory(logDirectory);
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                Path.Combine(logDirectory, logFileName),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("SSCA Data Migration Tool");
            Log.Information("========================");
            Log.Information("");

            // Validate configuration
            var sourceConnectionString = configuration["SourceDatabase:ConnectionString"];
            var targetConnectionString = configuration["TargetDatabase:ConnectionString"];
            var sourceWebsiteDomain = configuration["SourceWebsite:Domain"];
            var storageConnectionString = configuration["TargetStorage:ConnectionString"];
            var containerName = configuration["TargetStorage:ContainerName"];
            var progressFile = configuration["Migration:ProgressFile"] ?? "migration_progress.json";

            if (string.IsNullOrEmpty(sourceConnectionString))
            {
                Log.Error("SourceDatabase:ConnectionString is not configured");
                return 1;
            }

            if (string.IsNullOrEmpty(targetConnectionString))
            {
                Log.Error("TargetDatabase:ConnectionString is not configured");
                return 1;
            }

            if (string.IsNullOrEmpty(storageConnectionString) || storageConnectionString.Contains("<your-"))
            {
                Log.Error("TargetStorage:ConnectionString is not configured or contains placeholder values");
                return 1;
            }

            if (string.IsNullOrEmpty(sourceWebsiteDomain))
            {
                Log.Error("SourceWebsite:Domain is not configured");
                return 1;
            }

            // Parse settings
            var settings = new MigrationSettings
            {
                BatchSize = int.TryParse(configuration["Migration:BatchSize"], out var batch) ? batch : 10,
                RetryCount = int.TryParse(configuration["Migration:RetryCount"], out var retry) ? retry : 3,
                RetryDelaySeconds = int.TryParse(configuration["Migration:RetryDelaySeconds"], out var delay) ? delay : 5,
                DryRun = bool.TryParse(configuration["Migration:DryRun"], out var dryRun) && dryRun
            };

            Log.Information("Configuration:");
            Log.Information("  Source Database: MySQL (connection string configured)");
            Log.Information("  Target Database: PostgreSQL (connection string configured)");
            Log.Information("  Source Website: {Domain}", sourceWebsiteDomain);
            Log.Information("  Azure Storage Container: {Container}", containerName);
            Log.Information("  Progress File: {File}", progressFile);
            Log.Information("  Batch Size: {Size}", settings.BatchSize);
            Log.Information("  Retry Count: {Count}", settings.RetryCount);
            Log.Information("  Dry Run: {DryRun}", settings.DryRun);
            Log.Information("");

            // Create services
            using var sourceDb = new SourceDatabaseService(sourceConnectionString);
            using var targetDb = new TargetDatabaseService(targetConnectionString);
            using var audioService = new AudioMigrationService(
                sourceWebsiteDomain,
                configuration["SourceWebsite:SundayMessageAudioPath"] ?? "/messages/sundaymsg",
                configuration["SourceWebsite:SpecialMessageAudioPath"] ?? "/messages/specialmsg",
                storageConnectionString,
                containerName ?? "audio-files");
            var progressService = new ProgressService(progressFile);
            
            // Speaker normalization service (optional speaker_mappings.json file)
            var speakerMappingsFile = Path.Combine(Directory.GetCurrentDirectory(), "speaker_mappings.json");
            var speakerService = new SpeakerNormalizationService(speakerMappingsFile);

            // Create orchestrator and run migration
            var orchestrator = new MigrationOrchestrator(
                sourceDb, targetDb, audioService, progressService, speakerService, settings);

            var success = await orchestrator.RunMigrationAsync();

            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception during migration");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
