// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using JulesSdk.Exceptions;
using JulesSdk.Http;
using JulesSdk.Models;

namespace JulesSdk.Client;

/// <summary>
/// Implementation of the SessionClient interface.
/// </summary>
internal class SessionClientImpl : ISessionClient
{
    private readonly ApiClient _apiClient;
    private readonly int _pollingIntervalMs;
    
    public string Id { get; }
    
    public SessionClientImpl(string sessionId, ApiClient apiClient, int pollingIntervalMs)
    {
        Id = sessionId.Replace("sessions/", "");
        _apiClient = apiClient;
        _pollingIntervalMs = pollingIntervalMs;
    }
    
    public async IAsyncEnumerable<Activity> StreamAsync(
        StreamOptions? options = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? pageToken = null;
        var lastSeenTime = "";
        var seenIdsAtLastTime = new HashSet<string>();
        var consecutiveErrors = 0;
        const int maxConsecutiveErrors = 5;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            ListActivitiesResponse? response = null;
            
            try
            {
                var query = new Dictionary<string, string> { ["pageSize"] = "50" };
                if (!string.IsNullOrEmpty(pageToken))
                    query["pageToken"] = pageToken;
                
                // Apply createTime filter for server-side filtering
                var createTimeFilter = options?.GetCreateTimeFilter();
                if (!string.IsNullOrEmpty(createTimeFilter))
                    query["createTime"] = createTimeFilter;
                    
                response = await _apiClient.RequestAsync<ListActivitiesResponse>(
                    $"sessions/{Id}/activities",
                    new ApiRequestOptions { Query = query },
                    cancellationToken);
                    
                consecutiveErrors = 0; // Reset on success
            }
            catch (JulesNetworkException) when (consecutiveErrors < maxConsecutiveErrors)
            {
                // Retry transient network errors with backoff
                consecutiveErrors++;
                var delay = Math.Min(_pollingIntervalMs * Math.Pow(2, consecutiveErrors - 1), 30000);
                await Task.Delay((int)delay, cancellationToken);
                continue;
            }
            catch (JulesRateLimitException) when (consecutiveErrors < maxConsecutiveErrors)
            {
                // Retry rate limit errors with longer backoff
                consecutiveErrors++;
                var delay = Math.Min(5000 * Math.Pow(2, consecutiveErrors - 1), 60000);
                await Task.Delay((int)delay, cancellationToken);
                continue;
            }
            
            foreach (var activity in response?.Activities ?? [])
            {
                // Deduplication logic
                if (string.Compare(activity.CreateTime, lastSeenTime) < 0)
                    continue;
                    
                if (activity.CreateTime == lastSeenTime)
                {
                    if (seenIdsAtLastTime.Contains(activity.Id))
                        continue;
                    seenIdsAtLastTime.Add(activity.Id);
                }
                else
                {
                    lastSeenTime = activity.CreateTime;
                    seenIdsAtLastTime.Clear();
                    seenIdsAtLastTime.Add(activity.Id);
                }
                
                // Apply filter
                if (options?.ExcludeOriginator != null && activity.Originator == options.ExcludeOriginator)
                    continue;
                    
                yield return activity;
            }
            
            if (!string.IsNullOrEmpty(response?.NextPageToken))
            {
                pageToken = response.NextPageToken;
            }
            else
            {
                pageToken = null;
                await Task.Delay(_pollingIntervalMs, cancellationToken);
            }
        }
    }
    
    public async IAsyncEnumerable<Activity> HistoryAsync(
        StreamOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? pageToken = null;
        
        do
        {
            var query = new Dictionary<string, string> { ["pageSize"] = "100" };
            if (!string.IsNullOrEmpty(pageToken))
                query["pageToken"] = pageToken;
            
            // Apply createTime filter for server-side filtering
            var createTimeFilter = options?.GetCreateTimeFilter();
            if (!string.IsNullOrEmpty(createTimeFilter))
                query["createTime"] = createTimeFilter;
                
            var response = await _apiClient.RequestAsync<ListActivitiesResponse>(
                $"sessions/{Id}/activities",
                new ApiRequestOptions { Query = query },
                cancellationToken);
                
            foreach (var activity in response.Activities ?? [])
            {
                // Apply client-side filters
                if (options?.ExcludeOriginator != null && activity.Originator == options.ExcludeOriginator)
                    continue;
                    
                yield return activity;
            }
            
            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));
    }
    
    public async IAsyncEnumerable<Activity> UpdatesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Use server-side createTime filter for efficiency
        var startTime = DateTime.UtcNow;
        
        await foreach (var activity in StreamAsync(
            new StreamOptions { Since = startTime },
            cancellationToken))
        {
            if (DateTime.TryParse(activity.CreateTime, out var activityTime) && activityTime > startTime)
            {
                yield return activity;
            }
        }
    }
    
    public async Task ApproveAsync(CancellationToken cancellationToken = default)
    {
        // Don't pre-check state - just try the API call.
        // The API will return an error if the session is not in a valid state.
        // This handles "Inactive" sessions where API returns COMPLETED but
        // the session can still be resumed by approving the plan.
        await _apiClient.RequestAsync<object>(
            $"sessions/{Id}:approvePlan",
            new ApiRequestOptions { Method = HttpMethod.Post, Body = new { } },
            cancellationToken);
    }
    
    public async Task SendAsync(string prompt, CancellationToken cancellationToken = default)
    {
        await _apiClient.RequestAsync<object>(
            $"sessions/{Id}:sendMessage",
            new ApiRequestOptions { Method = HttpMethod.Post, Body = new { prompt } },
            cancellationToken);
    }
    
    public async Task<AgentMessagedActivity> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        await SendAsync(prompt, cancellationToken);
        
        await foreach (var activity in StreamAsync(new StreamOptions { ExcludeOriginator = Origin.User }, cancellationToken))
        {
            if (DateTime.TryParse(activity.CreateTime, out var activityTime) && activityTime <= startTime)
                continue;
            
            // Check if this is an agent messaged activity using the union field
            if (activity.IsAgentMessaged)
            {
                // Return as legacy type for backwards compatibility
                return new AgentMessagedActivity
                {
                    Name = activity.Name,
                    CreateTime = activity.CreateTime,
                    Originator = activity.Originator,
                    AgentMessaged = activity.AgentMessaged
                };
            }
        }
        
        throw new JulesException("Session ended before the agent replied.");
    }
    
    public async Task<SessionOutcome> ResultAsync(CancellationToken cancellationToken = default)
    {
        var session = await PollUntilCompletionAsync(cancellationToken);
        return MapToOutcome(session);
    }
    
    public async Task WaitForAsync(SessionState state, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var info = await InfoAsync(cancellationToken);
            
            if (info.State == state || 
                info.State == SessionState.Completed || 
                info.State == SessionState.Failed)
            {
                return;
            }
            
            await Task.Delay(_pollingIntervalMs, cancellationToken);
        }
    }
    
    public async Task<SessionResource> InfoAsync(CancellationToken cancellationToken = default)
    {
        var resource = await _apiClient.RequestAsync<SessionResource>(
            $"sessions/{Id}",
            cancellationToken: cancellationToken);
            
        // Map outcome from outputs if not already present
        if (resource.Outcome == null && resource.Outputs != null)
        {
            resource.Outcome = MapToOutcomeData(resource);
        }
        
        return resource;
    }
    
    private static SessionOutcomeData MapToOutcomeData(SessionResource session)
    {
        var pullRequest = session.Outputs?
            .FirstOrDefault(o => o.PullRequest != null)?
            .PullRequest;
            
        var changeSet = session.Outputs?
            .FirstOrDefault(o => o.ChangeSet != null)?
            .ChangeSet;
            
        return new SessionOutcomeData
        {
            PullRequest = pullRequest,
            ChangeSet = changeSet
        };
    }
    
    public async Task<Activity> GetActivityAsync(string activityId, CancellationToken cancellationToken = default)
    {
        // Strip prefix if provided
        var cleanId = activityId.Replace($"sessions/{Id}/activities/", "");
        return await _apiClient.RequestAsync<Activity>(
            $"sessions/{Id}/activities/{cleanId}",
            cancellationToken: cancellationToken);
    }
    
    public async Task<SessionSnapshot> SnapshotAsync(Options.SnapshotOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new Options.SnapshotOptions();
        
        // Fetch session info and activities in parallel
        var sessionTask = InfoAsync(cancellationToken);
        var activitiesTask = options.IncludeActivities 
            ? CollectActivitiesAsync(cancellationToken)
            : Task.FromResult<IReadOnlyList<Activity>>([]);
        
        await Task.WhenAll(sessionTask, activitiesTask);
        
        return new SessionSnapshot(sessionTask.Result, activitiesTask.Result);
    }
    
    private async Task<IReadOnlyList<Activity>> CollectActivitiesAsync(CancellationToken cancellationToken)
    {
        var activities = new List<Activity>();
        await foreach (var activity in HistoryAsync(null, cancellationToken))
        {
            activities.Add(activity);
        }
        return activities;
    }
    
    private async Task<SessionResource> PollUntilCompletionAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var session = await InfoAsync(cancellationToken);
            
            if (session.State is SessionState.Completed or SessionState.Failed)
                return session;
                
            await Task.Delay(_pollingIntervalMs, cancellationToken);
        }
        
        throw new OperationCanceledException();
    }
    
    private static SessionOutcome MapToOutcome(SessionResource session)
    {
        var pullRequest = session.Outcome?.PullRequest ?? session.Outputs?
            .FirstOrDefault(o => o.PullRequest != null)?
            .PullRequest;
            
        var changeSet = session.Outcome?.ChangeSet ?? session.Outputs?
            .FirstOrDefault(o => o.ChangeSet != null)?
            .ChangeSet;
            
        return new SessionOutcome
        {
            SessionId = session.Id,
            Title = session.Title ?? "",
            State = session.State,
            PullRequest = pullRequest,
            ChangeSetData = changeSet,
            Outputs = session.Outputs ?? []
        };
    }
    
    private record ListActivitiesResponse(
        IReadOnlyList<Activity>? Activities,
        string? NextPageToken);
}
