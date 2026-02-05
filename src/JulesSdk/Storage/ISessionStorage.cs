// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Models;

namespace JulesSdk.Storage;

/// <summary>
/// Interface for storage of sessions and activities.
/// </summary>
public interface ISessionStorage
{
    // Session operations
    Task<SessionResource?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task UpsertSessionAsync(SessionResource session, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<SessionResource> ListSessionsAsync(CancellationToken cancellationToken = default);
    
    // Activity operations
    IAsyncEnumerable<Activity> ListActivitiesAsync(string sessionId, CancellationToken cancellationToken = default);
    Task UpsertActivityAsync(string sessionId, Activity activity, CancellationToken cancellationToken = default);
    Task DeleteActivityAsync(string sessionId, string activityId, CancellationToken cancellationToken = default);

    // KV operations for state persistence
    Task SetActiveSessionAsync(string key, string sessionId, CancellationToken cancellationToken = default);
    Task<string?> GetActiveSessionAsync(string key, CancellationToken cancellationToken = default);
    Task ClearActiveSessionAsync(string key, CancellationToken cancellationToken = default);
}
