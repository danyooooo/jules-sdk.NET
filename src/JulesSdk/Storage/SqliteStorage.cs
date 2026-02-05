// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;
using JulesSdk.Models;
using Microsoft.Data.Sqlite;

namespace JulesSdk.Storage;

/// <summary>
/// SQLite-based implementation of session storage.
/// Provides thread-safe, transactional persistence for sessions and activities.
/// </summary>
public class SqliteStorage : ISessionStorage, IDisposable
{
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _initialized;

    public SqliteStorage(string dbPath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };
        _connectionString = builder.ToString();
        
        _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        using var transaction = connection.BeginTransaction();
        try
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Sessions (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL UNIQUE,
                    CreateTime TEXT,
                    JsonData TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Activities (
                    Id TEXT PRIMARY KEY,
                    SessionId TEXT NOT NULL,
                    CreateTime TEXT,
                    JsonData TEXT NOT NULL,
                    FOREIGN KEY(SessionId) REFERENCES Sessions(Id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS IDX_Activities_SessionId ON Activities(SessionId);
                CREATE INDEX IF NOT EXISTS IDX_Activities_CreateTime ON Activities(CreateTime);

                CREATE TABLE IF NOT EXISTS KVStore (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );
            ";
            
            await command.ExecuteNonQueryAsync(ct);
            transaction.Commit();
            _initialized = true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<SessionResource?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        // Handle "sessions/" prefix
        var cleanId = sessionId.Replace("sessions/", "");
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT JsonData FROM Sessions WHERE Id = @Id OR Name = @Name";
        command.Parameters.AddWithValue("@Id", cleanId);
        command.Parameters.AddWithValue("@Name", sessionId.StartsWith("sessions/") ? sessionId : $"sessions/{sessionId}");
        
        var json = await command.ExecuteScalarAsync(cancellationToken) as string;
        if (json == null) return null;
        
        return JsonSerializer.Deserialize<SessionResource>(json, _jsonOptions);
    }

    public async Task UpsertSessionAsync(SessionResource session, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        
        command.CommandText = @"
            INSERT INTO Sessions (Id, Name, CreateTime, JsonData)
            VALUES (@Id, @Name, @CreateTime, @JsonData)
            ON CONFLICT(Id) DO UPDATE SET
                Name = excluded.Name,
                CreateTime = excluded.CreateTime,
                JsonData = excluded.JsonData;
        ";
        
        command.Parameters.AddWithValue("@Id", session.Id);
        command.Parameters.AddWithValue("@Name", session.Name);
        command.Parameters.AddWithValue("@CreateTime", session.CreateTime ?? "");
        command.Parameters.AddWithValue("@JsonData", JsonSerializer.Serialize(session, _jsonOptions));
        
        await command.ExecuteNonQueryAsync(cancellationToken);
        transaction.Commit();
    }

    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        var cleanId = sessionId.Replace("sessions/", "");
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Sessions WHERE Id = @Id OR Name = @Name";
        command.Parameters.AddWithValue("@Id", cleanId);
        command.Parameters.AddWithValue("@Name", sessionId);
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async IAsyncEnumerable<SessionResource> ListSessionsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT JsonData FROM Sessions ORDER BY CreateTime DESC";
        
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var json = reader.GetString(0);
            var session = JsonSerializer.Deserialize<SessionResource>(json, _jsonOptions);
            if (session != null)
            {
                yield return session;
            }
        }
    }

    public async IAsyncEnumerable<Activity> ListActivitiesAsync(
        string sessionId, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        var cleanId = sessionId.Replace("sessions/", "");
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT JsonData FROM Activities WHERE SessionId = @SessionId ORDER BY CreateTime ASC";
        command.Parameters.AddWithValue("@SessionId", cleanId);
        
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var json = reader.GetString(0);
            var activity = JsonSerializer.Deserialize<Activity>(json, _jsonOptions);
            if (activity != null)
            {
                yield return activity;
            }
        }
    }

    public async Task UpsertActivityAsync(string sessionId, Activity activity, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        var cleanSessionId = sessionId.Replace("sessions/", "");
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        
        command.CommandText = @"
            INSERT INTO Activities (Id, SessionId, CreateTime, JsonData)
            VALUES (@Id, @SessionId, @CreateTime, @JsonData)
            ON CONFLICT(Id) DO UPDATE SET
                CreateTime = excluded.CreateTime,
                JsonData = excluded.JsonData;
        ";
        
        command.Parameters.AddWithValue("@Id", activity.Id);
        command.Parameters.AddWithValue("@SessionId", cleanSessionId);
        command.Parameters.AddWithValue("@CreateTime", activity.CreateTime ?? "");
        command.Parameters.AddWithValue("@JsonData", JsonSerializer.Serialize(activity, _jsonOptions));
        
        await command.ExecuteNonQueryAsync(cancellationToken);
        transaction.Commit();
    }

    public async Task DeleteActivityAsync(string sessionId, string activityId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Activities WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", activityId);
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetActiveSessionAsync(string key, string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO KVStore (Key, Value) VALUES (@Key, @Value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
        ";
        command.Parameters.AddWithValue("@Key", key);
        command.Parameters.AddWithValue("@Value", sessionId);
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<string?> GetActiveSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM KVStore WHERE Key = @Key";
        command.Parameters.AddWithValue("@Key", key);
        
        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }

    public async Task ClearActiveSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM KVStore WHERE Key = @Key";
        command.Parameters.AddWithValue("@Key", key);
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public void Dispose()
    {
        // No persistent resources to dispose, connections are disposed per-method
        GC.SuppressFinalize(this);
    }
}
