// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using JulesSdk.Models;

namespace JulesSdk.Storage;

/// <summary>
/// In-memory implementation of session storage.
/// </summary>
public class MemoryStorage : ISessionStorage
{
    private readonly ConcurrentDictionary<string, SessionResource> _sessions = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Activity>> _activities = new();
    
    public Task<SessionResource?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }
    
    public Task UpsertSessionAsync(SessionResource session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Name] = session;
        return Task.CompletedTask;
    }
    
    public Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(sessionId, out _);
        _activities.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
    
    public async IAsyncEnumerable<SessionResource> ListSessionsAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var session in _sessions.Values)
        {
            yield return session;
        }
        await Task.CompletedTask;
    }
    
    public async IAsyncEnumerable<Activity> ListActivitiesAsync(
        string sessionId, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_activities.TryGetValue(sessionId, out var activities))
        {
            // Return sorted by creation time
            var sorted = activities.Values.OrderBy(a => a.CreateTime).ToList();
            foreach (var activity in sorted)
            {
                yield return activity;
            }
        }
        await Task.CompletedTask;
    }
    
    public Task UpsertActivityAsync(string sessionId, Activity activity, CancellationToken cancellationToken = default)
    {
        var sessionActivities = _activities.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, Activity>());
        sessionActivities[activity.Id] = activity;
        return Task.CompletedTask;
    }
    
    public Task DeleteActivityAsync(string sessionId, string activityId, CancellationToken cancellationToken = default)
    {
        if (_activities.TryGetValue(sessionId, out var activities))
        {
            activities.TryRemove(activityId, out _);
        }
        return Task.CompletedTask;
    }
    
    private readonly ConcurrentDictionary<string, string> _kvStore = new();
    
    public Task SetActiveSessionAsync(string key, string sessionId, CancellationToken cancellationToken = default)
    {
        _kvStore[key] = sessionId;
        return Task.CompletedTask;
    }

    public Task<string?> GetActiveSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        _kvStore.TryGetValue(key, out var val);
        return Task.FromResult(val);
    }

    public Task ClearActiveSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        _kvStore.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
