using MySqlConnector;
using SSCA.DataMigration.Models;
using Serilog;

namespace SSCA.DataMigration.Services;

/// <summary>
/// Service for reading data from the source MySQL database
/// </summary>
public class SourceDatabaseService : IDisposable
{
    private readonly string _connectionString;

    public SourceDatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Test the connection to the source database
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            Log.Information("Successfully connected to source MySQL database");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to connect to source MySQL database");
            return false;
        }
    }

    /// <summary>
    /// Get all Sunday messages from ssca_sunday_msg table
    /// </summary>
    public async Task<List<SourceSundayMessage>> GetSundayMessagesAsync()
    {
        var messages = new List<SourceSundayMessage>();
        
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT id, date, date_ts, speaker, theme, gospel, audio_file 
            FROM ssca_sunday_msg 
            ORDER BY id";

        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            messages.Add(new SourceSundayMessage
            {
                Id = reader.GetInt32("id"),
                Date = reader.GetString("date"),
                DateTs = reader.GetInt32("date_ts"),
                Speaker = reader.GetString("speaker"),
                Theme = reader.GetString("theme"),
                Gospel = reader.GetInt32("gospel"),
                AudioFile = reader.GetString("audio_file")
            });
        }

        Log.Information("Loaded {Count} Sunday messages from source database", messages.Count);
        return messages;
    }

    /// <summary>
    /// Get all Special messages from ssca_special_msg table
    /// </summary>
    public async Task<List<SourceSpecialMessage>> GetSpecialMessagesAsync()
    {
        var messages = new List<SourceSpecialMessage>();
        
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT id, date, date_ts, speaker, theme, gospel, audio_file 
            FROM ssca_special_msg 
            ORDER BY id";

        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            messages.Add(new SourceSpecialMessage
            {
                Id = reader.GetInt32("id"),
                Date = reader.GetString("date"),
                DateTs = reader.GetInt32("date_ts"),
                Speaker = reader.GetString("speaker"),
                Theme = reader.GetString("theme"),
                Gospel = reader.GetInt32("gospel"),
                AudioFile = reader.GetString("audio_file")
            });
        }

        Log.Information("Loaded {Count} Special messages from source database", messages.Count);
        return messages;
    }

    /// <summary>
    /// Get count of Sunday messages
    /// </summary>
    public async Task<int> GetSundayMessageCountAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = "SELECT COUNT(*) FROM ssca_sunday_msg";
        await using var cmd = new MySqlCommand(sql, connection);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Get count of Special messages
    /// </summary>
    public async Task<int> GetSpecialMessageCountAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = "SELECT COUNT(*) FROM ssca_special_msg";
        await using var cmd = new MySqlCommand(sql, connection);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public void Dispose()
    {
        // Connections are disposed per operation (using await using)
    }
}
