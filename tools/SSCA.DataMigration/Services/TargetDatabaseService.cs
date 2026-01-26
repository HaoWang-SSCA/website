using Npgsql;
using SSCA.DataMigration.Models;
using Serilog;

namespace SSCA.DataMigration.Services;

/// <summary>
/// Service for writing data to the target PostgreSQL database
/// </summary>
public class TargetDatabaseService : IDisposable
{
    private readonly string _connectionString;

    public TargetDatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Test the connection to the target database
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            Log.Information("Successfully connected to target PostgreSQL database");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to connect to target PostgreSQL database");
            return false;
        }
    }

    /// <summary>
    /// Check if a record already exists in the target (by date, speaker, topic combination)
    /// </summary>
    public async Task<Guid?> FindExistingRecordAsync(DateTime date, string speaker, string topic, 
        bool isGospel, bool isSpecialMeeting)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT ""Id"" FROM ""MessageMeetings"" 
            WHERE ""Date"" = @date 
            AND ""Speaker"" = @speaker 
            AND ""Topic"" = @topic
            AND ""IsGospel"" = @isGospel
            AND ""IsSpecialMeeting"" = @isSpecialMeeting
            LIMIT 1";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("date", date);
        cmd.Parameters.AddWithValue("speaker", speaker);
        cmd.Parameters.AddWithValue("topic", topic);
        cmd.Parameters.AddWithValue("isGospel", isGospel);
        cmd.Parameters.AddWithValue("isSpecialMeeting", isSpecialMeeting);

        var result = await cmd.ExecuteScalarAsync();
        return result != null ? (Guid)result : null;
    }

    /// <summary>
    /// Check if a record exists by date and topic (ignoring speaker, for re-migration/normalization)
    /// </summary>
    public async Task<Guid?> FindExistingRecordByDateAndTopicAsync(DateTime date, string topic, 
        bool isGospel, bool isSpecialMeeting)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT ""Id"" FROM ""MessageMeetings"" 
            WHERE ""Date"" = @date 
            AND ""Topic"" = @topic
            AND ""IsGospel"" = @isGospel
            AND ""IsSpecialMeeting"" = @isSpecialMeeting
            LIMIT 1";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("date", date);
        cmd.Parameters.AddWithValue("topic", topic);
        cmd.Parameters.AddWithValue("isGospel", isGospel);
        cmd.Parameters.AddWithValue("isSpecialMeeting", isSpecialMeeting);

        var result = await cmd.ExecuteScalarAsync();
        return result != null ? (Guid)result : null;
    }

    /// <summary>
    /// Insert a new message meeting record
    /// </summary>
    public async Task<Guid> InsertMessageMeetingAsync(TargetMessageMeeting meeting)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO ""MessageMeetings"" 
            (""Id"", ""Date"", ""Speaker"", ""Topic"", ""AudioBlobName"", ""VideoUrl"", 
             ""IsGospel"", ""IsSpecialMeeting"", ""CreatedAt"", ""UpdatedAt"")
            VALUES 
            (@id, @date, @speaker, @topic, @audioBlobName, @videoUrl, 
             @isGospel, @isSpecialMeeting, @createdAt, @updatedAt)
            RETURNING ""Id""";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("id", meeting.Id);
        cmd.Parameters.AddWithValue("date", meeting.Date);
        cmd.Parameters.AddWithValue("speaker", meeting.Speaker);
        cmd.Parameters.AddWithValue("topic", meeting.Topic);
        cmd.Parameters.AddWithValue("audioBlobName", (object?)meeting.AudioBlobName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("videoUrl", (object?)meeting.VideoUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("isGospel", meeting.IsGospel);
        cmd.Parameters.AddWithValue("isSpecialMeeting", meeting.IsSpecialMeeting);
        cmd.Parameters.AddWithValue("createdAt", meeting.CreatedAt);
        cmd.Parameters.AddWithValue("updatedAt", meeting.UpdatedAt);

        var result = await cmd.ExecuteScalarAsync();
        return (Guid)result!;
    }

    /// <summary>
    /// Update the AudioBlobName for an existing record
    /// </summary>
    public async Task UpdateAudioBlobNameAsync(Guid id, string audioBlobName)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            UPDATE ""MessageMeetings"" 
            SET ""AudioBlobName"" = @audioBlobName, ""UpdatedAt"" = @updatedAt
            WHERE ""Id"" = @id";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("audioBlobName", audioBlobName);
        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Update the VideoUrl for an existing record
    /// </summary>
    public async Task UpdateVideoUrlAsync(Guid id, string? videoUrl)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            UPDATE ""MessageMeetings"" 
            SET ""VideoUrl"" = @videoUrl, ""UpdatedAt"" = @updatedAt
            WHERE ""Id"" = @id";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("videoUrl", (object?)videoUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Update the Speaker for an existing record
    /// </summary>
    public async Task UpdateSpeakerAsync(Guid id, string speaker)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            UPDATE ""MessageMeetings"" 
            SET ""Speaker"" = @speaker, ""UpdatedAt"" = @updatedAt
            WHERE ""Id"" = @id";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("speaker", speaker);
        cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Get count of records in target database
    /// </summary>
    public async Task<int> GetRecordCountAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"SELECT COUNT(*) FROM ""MessageMeetings""";
        await using var cmd = new NpgsqlCommand(sql, connection);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public void Dispose()
    {
        // Connection is disposed per operation
    }
}
