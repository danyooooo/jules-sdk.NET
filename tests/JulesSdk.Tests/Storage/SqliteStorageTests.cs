// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Models;
using JulesSdk.Storage;
using Xunit;

namespace JulesSdk.Tests.Storage;

public class SqliteStorageTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteStorage _storage;

    public SqliteStorageTests()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "jules_test_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        _dbPath = Path.Combine(tempDir, "test.db");
        _storage = new SqliteStorage(_dbPath);
    }

    public void Dispose()
    {
        _storage.Dispose();
        var dir = Path.GetDirectoryName(_dbPath);
        if (dir != null && Directory.Exists(dir))
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    private static SessionResource CreateTestSession(string name = "sessions/123") => new()
    {
        Name = name,
        Prompt = "Test prompt",
        CreateTime = "2026-01-01T00:00:00Z",
        UpdateTime = "2026-01-01T00:00:00Z"
    };

    [Fact]
    public async Task Session_Cycle_Works()
    {
        var session = CreateTestSession("sessions/123");

        // Insert
        await _storage.UpsertSessionAsync(session);

        // Get
        var retrieved = await _storage.GetSessionAsync("sessions/123");
        Assert.NotNull(retrieved);
        Assert.Equal(session.Name, retrieved!.Name);
        
        // List
        var sessions = new List<SessionResource>();
        await foreach (var s in _storage.ListSessionsAsync())
            sessions.Add(s);
        Assert.Single(sessions);
        Assert.Equal(session.Name, sessions[0].Name);

        // Delete
        await _storage.DeleteSessionAsync("sessions/123");
        retrieved = await _storage.GetSessionAsync("sessions/123");
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task KV_Store_Works()
    {
        const string key = "my-resume-key";
        const string sessionId = "sessions/abc-def";

        // Set
        await _storage.SetActiveSessionAsync(key, sessionId);

        // Get
        var retrieved = await _storage.GetActiveSessionAsync(key);
        Assert.Equal(sessionId, retrieved);

        // Clear
        await _storage.ClearActiveSessionAsync(key);
        retrieved = await _storage.GetActiveSessionAsync(key);
        Assert.Null(retrieved);
    }
    
    [Fact]
    public async Task Concurrent_Writes_Are_Safe()
    {
        // This test verifies SQLite locking handles concurrency
        var tasks = new List<Task>();
        const int count = 50;
        
        for (int i = 0; i < count; i++)
        {
            var id = i;
            tasks.Add(Task.Run(async () => 
            {
                var session = CreateTestSession($"sessions/{id}");
                await _storage.UpsertSessionAsync(session);
            }));
        }
        
        await Task.WhenAll(tasks);
        
        var sessions = new List<SessionResource>();
        await foreach (var s in _storage.ListSessionsAsync())
            sessions.Add(s);
            
        Assert.Equal(count, sessions.Count);
    }
}
