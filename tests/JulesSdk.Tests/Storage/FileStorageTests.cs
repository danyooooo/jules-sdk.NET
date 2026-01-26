// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Models;
using JulesSdk.Storage;
using Xunit;

namespace JulesSdk.Tests.Storage;

public class FileStorageTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileStorage _storage;

    public FileStorageTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "jules_test_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        _storage = new FileStorage(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private static SessionResource CreateTestSession(string name = "sessions/123") => new()
    {
        Name = name,
        Prompt = "Test prompt",
        CreateTime = "2026-01-01T00:00:00Z",
        UpdateTime = "2026-01-01T00:00:00Z"
    };

    [Fact]
    public async Task Session_Persists_To_Disk()
    {
        var session = CreateTestSession("sessions/123");

        // Insert
        await _storage.UpsertSessionAsync(session);

        // Verify file exists (sanitized: sessions/123 -> sessions_123)
        var sanitizedId = "sessions_123";
        var sessionFile = Path.Combine(_tempDir, "sessions", $"{sanitizedId}.json");
        Assert.True(File.Exists(sessionFile), $"Session file should exist: {sessionFile}");

        // Re-read from new instance
        var storage2 = new FileStorage(_tempDir);
        var retrieved = await storage2.GetSessionAsync("sessions/123");
        Assert.NotNull(retrieved);
        Assert.Equal(session.Name, retrieved!.Name);
    }

    [Fact]
    public async Task Activity_Persists_To_Disk()
    {
        const string sessionId = "sessions/123";
        var activity = new AgentMessagedActivity
        {
            Name = "sessions/123/activities/abc",
            CreateTime = "2026-01-01T00:00:00Z",
            Message = "Test message"
        };

        await _storage.UpsertActivityAsync(sessionId, activity);

        // Verify file (sanitized paths)
        var sanitizedSessionId = "sessions_123";
        var sanitizedActivityId = "sessions_123_activities_abc";
        var activityFile = Path.Combine(_tempDir, "activities", sanitizedSessionId, $"{sanitizedActivityId}.json");
        Assert.True(File.Exists(activityFile), $"Activity file should exist: {activityFile}");

        // List from new instance
        var storage2 = new FileStorage(_tempDir);
        var activities = new List<Activity>();
        await foreach (var a in storage2.ListActivitiesAsync(sessionId))
            activities.Add(a);
        Assert.Single(activities);
        Assert.Equal(activity.Name, activities[0].Name);
    }
}
