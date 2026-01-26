// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Models;
using JulesSdk.Storage;
using Xunit;

namespace JulesSdk.Tests.Storage;

public class MemoryStorageTests
{
    private readonly MemoryStorage _storage = new();

    [Fact]
    public async Task Session_Cycle_Works()
    {
        var session = new SessionResource { Id = "sessions/123", CreateTime = DateTime.UtcNow };

        // Insert
        await _storage.UpsertSessionAsync(session);

        // Get
        var retrieved = await _storage.GetSessionAsync("sessions/123");
        Assert.NotNull(retrieved);
        Assert.Equal(session.Id, retrieved!.Id);

        // List
        var sessions = await _storage.ListSessionsAsync().ToListAsync();
        Assert.Single(sessions);
        Assert.Equal(session.Id, sessions[0].Id);

        // Delete
        await _storage.DeleteSessionAsync("sessions/123");
        retrieved = await _storage.GetSessionAsync("sessions/123");
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task Activity_Cycle_Works()
    {
        const string sessionId = "sessions/123";
        var activity1 = new Activity { Id = "activities/1", CreateTime = DateTime.UtcNow };
        var activity2 = new Activity { Id = "activities/2", CreateTime = DateTime.UtcNow.AddMinutes(1) };

        // Insert
        await _storage.UpsertActivityAsync(sessionId, activity1);
        await _storage.UpsertActivityAsync(sessionId, activity2);

        // List
        var activities = await _storage.ListActivitiesAsync(sessionId).ToListAsync();
        Assert.Equal(2, activities.Count);
        Assert.Equal("activities/1", activities[0].Id);
        Assert.Equal("activities/2", activities[1].Id);

        // Delete
        await _storage.DeleteActivityAsync(sessionId, "activities/1");
        activities = await _storage.ListActivitiesAsync(sessionId).ToListAsync();
        Assert.Single(activities);
        Assert.Equal("activities/2", activities[0].Id);
    }
}
