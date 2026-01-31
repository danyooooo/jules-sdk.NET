// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Models;
using JulesSdk.Options;

namespace JulesSdk;

/// <summary>
/// Options for listing sessions.
/// </summary>
public class ListSessionsOptions
{
    /// <summary>
    /// Number of sessions per page.
    /// </summary>
    public int PageSize { get; init; } = 20;
    
    /// <summary>
    /// Maximum total sessions to return.
    /// </summary>
    public int? Limit { get; init; }
    
    /// <summary>
    /// Page token for pagination.
    /// </summary>
    public string? PageToken { get; init; }
}

/// <summary>
/// Options for batch operations.
/// </summary>
public class BatchOptions
{
    /// <summary>
    /// Maximum number of concurrent sessions to run.
    /// </summary>
    public int Concurrency { get; init; } = 4;
    
    /// <summary>
    /// If true, stop immediately if any session fails to start.
    /// </summary>
    public bool StopOnError { get; init; } = true;
    
    /// <summary>
    /// Delay in milliseconds between starting each session.
    /// </summary>
    public int DelayMs { get; init; } = 0;
}

/// <summary>
/// Options for sync operations.
/// </summary>
public class SyncOptions
{
    /// <summary>
    /// If set, syncs only this specific session.
    /// </summary>
    public string? SessionId { get; init; }
    
    /// <summary>
    /// Maximum number of sessions to ingest in one pass.
    /// </summary>
    public int Limit { get; init; } = 100;
    
    /// <summary>
    /// Data depth per session: "metadata" or "activities".
    /// </summary>
    public SyncDepth Depth { get; init; } = SyncDepth.Metadata;
    
    /// <summary>
    /// If true, stops when hitting a record already in the local cache.
    /// </summary>
    public bool Incremental { get; init; } = true;
    
    /// <summary>
    /// Simultaneous hydration jobs.
    /// </summary>
    public int Concurrency { get; init; } = 3;
    
    /// <summary>
    /// Cancellation token for graceful cancellation.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}

/// <summary>
/// Defines the depth of data ingestion.
/// </summary>
public enum SyncDepth
{
    /// <summary>Only SessionResource fields (lightweight).</summary>
    Metadata,
    
    /// <summary>Full hydration including all event logs (heavyweight).</summary>
    Activities
}

/// <summary>
/// Metrics resulting from a completed sync job.
/// </summary>
public record SyncStats(
    int SessionsIngested,
    int ActivitiesIngested,
    bool IsComplete,
    long DurationMs
);

/// <summary>
/// An automated session handle returned by RunAsync.
/// </summary>
public interface IAutomatedSession
{
    /// <summary>
    /// The unique ID of the session.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Provides a real-time stream of activities.
    /// </summary>
    IAsyncEnumerable<Activity> StreamAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Waits for the session to complete and returns the final outcome.
    /// </summary>
    Task<SessionOutcome> ResultAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for streaming activities.
/// </summary>
public class StreamOptions
{
    /// <summary>
    /// Filter to exclude activities by originator.
    /// </summary>
    public Origin? ExcludeOriginator { get; init; }
    
    /// <summary>
    /// Only return activities created at or after this time.
    /// Uses the API's createTime filter for efficient server-side filtering.
    /// </summary>
    public DateTime? Since { get; init; }
    
    /// <summary>
    /// Only return activities created at or after this RFC 3339 timestamp.
    /// Takes precedence over <see cref="Since"/> if both are set.
    /// Example: "2026-01-17T00:03:53.137240Z"
    /// </summary>
    public string? SinceTimestamp { get; init; }
    
    /// <summary>
    /// Gets the timestamp string for the API query.
    /// </summary>
    internal string? GetCreateTimeFilter()
    {
        if (!string.IsNullOrEmpty(SinceTimestamp))
            return SinceTimestamp;
        
        if (Since.HasValue)
            return Since.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ");
        
        return null;
    }
}

/// <summary>
/// Represents an active, interactive session with the Jules agent.
/// </summary>
public interface ISessionClient
{
    /// <summary>
    /// The unique ID of the session.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Provides a real-time stream of activities for the session.
    /// </summary>
    IAsyncEnumerable<Activity> StreamAsync(StreamOptions? options = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Yields all known past activities from local storage.
    /// </summary>
    /// <param name="options">Optional filtering options including createTime filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    IAsyncEnumerable<Activity> HistoryAsync(StreamOptions? options = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Yields only future activities as they arrive from the network.
    /// </summary>
    IAsyncEnumerable<Activity> UpdatesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Approves the currently pending plan.
    /// </summary>
    Task ApproveAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a message (prompt) to the agent.
    /// </summary>
    Task SendAsync(string prompt, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a message to the agent and waits for the agent's reply.
    /// </summary>
    Task<AgentMessagedActivity> AskAsync(string prompt, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Waits for the session to complete and returns the result.
    /// </summary>
    Task<SessionOutcome> ResultAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Waits until the session reaches a specific state.
    /// </summary>
    Task WaitForAsync(SessionState state, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves the latest state of the session resource.
    /// </summary>
    Task<SessionResource> InfoAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a specific activity by its ID.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The activity resource.</returns>
    Task<Activity> GetActivityAsync(string activityId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a point-in-time snapshot of the session with all activities loaded and derived analytics computed.
    /// </summary>
    /// <param name="options">Optional snapshot options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A SessionSnapshot with analytics, timeline, and insights.</returns>
    Task<SessionSnapshot> SnapshotAsync(SnapshotOptions? options = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages source connections (e.g., GitHub repositories).
/// </summary>
public interface ISourceManager
{
    /// <summary>
    /// Iterates over all connected sources.
    /// </summary>
    IAsyncEnumerable<Source> ListAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Locates a specific source based on GitHub identifier (owner/repo format).
    /// Searches through all sources.
    /// </summary>
    Task<Source?> GetAsync(string github, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a source directly by its resource name.
    /// </summary>
    /// <param name="sourceName">The full resource name (e.g., "sources/abc123").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The source resource.</returns>
    Task<Source> GetByNameAsync(string sourceName, CancellationToken cancellationToken = default);
}

/// <summary>
/// The main client interface for interacting with the Jules API.
/// </summary>
public interface IJulesClient : IDisposable
{
    /// <summary>
    /// Executes a task in automated mode.
    /// </summary>
    Task<IAutomatedSession> RunAsync(SessionConfig config, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new interactive session.
    /// </summary>
    Task<ISessionClient> SessionAsync(SessionConfig config, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rehydrates an existing session from its ID.
    /// </summary>
    ISessionClient Session(string sessionId);
    
    /// <summary>
    /// Provides access to the Source Management interface.
    /// </summary>
    ISourceManager Sources { get; }
    
    /// <summary>
    /// Provides access to the local storage/cache.
    /// </summary>
    Storage.ISessionStorage Storage { get; }
    
    /// <summary>
    /// Lists sessions with pagination support.
    /// </summary>
    IAsyncEnumerable<SessionResource> SessionsAsync(ListSessionsOptions? options = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a batch of automated sessions in parallel.
    /// </summary>
    Task<IReadOnlyList<IAutomatedSession>> AllAsync<T>(
        IEnumerable<T> items, 
        Func<T, SessionConfig> mapper, 
        BatchOptions? options = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Synchronizes the local cache with the API.
    /// </summary>
    Task<SyncStats> SyncAsync(SyncOptions? options = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new client instance with updated configuration.
    /// </summary>
    IJulesClient With(JulesOptions options);
}
