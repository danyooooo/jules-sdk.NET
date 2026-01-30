// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Models;
using JulesSdk.Storage;
using Xunit;

namespace JulesSdk.Tests.Storage;

public class MemoryStorageTests
{
    private readonly MemoryStorage _storage = new();

    private static SessionResource CreateTestSession(string name = "sessions/123") => new()
    {
        Name = name,
        Prompt = "Test prompt",
        CreateTime = "2026-01-01T00:00:00Z",
        UpdateTime = "2026-01-01T00:00:00Z"
    };

    private static Activity CreateTestActivity(string name = "sessions/123/activities/1") => new()
    {
        Name = name,
        CreateTime = "2026-01-01T00:00:00Z",
        AgentMessaged = new AgentMessagedData { AgentMessage = "Test message" }
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
        Assert.Equal("123", retrieved.Id);

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
    public async Task Activity_Cycle_Works()
    {
        const string sessionId = "sessions/123";
        var activity1 = CreateTestActivity("sessions/123/activities/1");
        var activity2 = new Activity
        {
            Name = "sessions/123/activities/2",
            CreateTime = "2026-01-01T00:01:00Z",
            AgentMessaged = new AgentMessagedData { AgentMessage = "Test 2" }
        };

        // Insert
        await _storage.UpsertActivityAsync(sessionId, activity1);
        await _storage.UpsertActivityAsync(sessionId, activity2);

        // List
        var activities = new List<Activity>();
        await foreach (var a in _storage.ListActivitiesAsync(sessionId))
            activities.Add(a);
        Assert.Equal(2, activities.Count);

        // Delete
        await _storage.DeleteActivityAsync(sessionId, activity1.Id);
        activities.Clear();
        await foreach (var a in _storage.ListActivitiesAsync(sessionId))
            activities.Add(a);
        Assert.Single(activities);
    }
}
