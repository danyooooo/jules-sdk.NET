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
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var query = new Dictionary<string, string> { ["pageSize"] = "50" };
            if (!string.IsNullOrEmpty(pageToken))
                query["pageToken"] = pageToken;
                
            var response = await _apiClient.RequestAsync<ListActivitiesResponse>(
                $"sessions/{Id}/activities",
                new ApiRequestOptions { Query = query },
                cancellationToken);
                
            foreach (var activity in response.Activities ?? [])
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
            
            if (!string.IsNullOrEmpty(response.NextPageToken))
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
    
    public async IAsyncEnumerable<Activity> HistoryAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? pageToken = null;
        
        do
        {
            var query = new Dictionary<string, string> { ["pageSize"] = "100" };
            if (!string.IsNullOrEmpty(pageToken))
                query["pageToken"] = pageToken;
                
            var response = await _apiClient.RequestAsync<ListActivitiesResponse>(
                $"sessions/{Id}/activities",
                new ApiRequestOptions { Query = query },
                cancellationToken);
                
            foreach (var activity in response.Activities ?? [])
            {
                yield return activity;
            }
            
            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));
    }
    
    public async IAsyncEnumerable<Activity> UpdatesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // First, get the current time marker
        var startTime = DateTime.UtcNow;
        
        await foreach (var activity in StreamAsync(cancellationToken: cancellationToken))
        {
            if (DateTime.TryParse(activity.CreateTime, out var activityTime) && activityTime > startTime)
            {
                yield return activity;
            }
        }
    }
    
    public async Task ApproveAsync(CancellationToken cancellationToken = default)
    {
        var info = await InfoAsync(cancellationToken);
        if (info.State != SessionState.AwaitingPlanApproval)
        {
            throw new InvalidStateException(
                $"Cannot approve plan because the session is not awaiting approval. Current state: {info.State}");
        }
        
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
                
            if (activity is AgentMessagedActivity agentActivity)
                return agentActivity;
        }
        
        throw new JulesException("Session ended before the agent replied.");
    }
    
    public async Task<Outcome> ResultAsync(CancellationToken cancellationToken = default)
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
        return await _apiClient.RequestAsync<SessionResource>(
            $"sessions/{Id}",
            cancellationToken: cancellationToken);
    }
    
    public async Task<Activity> GetActivityAsync(string activityId, CancellationToken cancellationToken = default)
    {
        // Strip prefix if provided
        var cleanId = activityId.Replace($"sessions/{Id}/activities/", "");
        return await _apiClient.RequestAsync<Activity>(
            $"sessions/{Id}/activities/{cleanId}",
            cancellationToken: cancellationToken);
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
    
    private static Outcome MapToOutcome(SessionResource session)
    {
        var pullRequest = session.Outputs?
            .FirstOrDefault(o => o.Type == "pullRequest")?
            .PullRequest;
            
        return new Outcome
        {
            SessionId = session.Id,
            Title = session.Title ?? "",
            State = session.State,
            PullRequest = pullRequest,
            Outputs = session.Outputs ?? []
        };
    }
    
    private record ListActivitiesResponse(
        IReadOnlyList<Activity>? Activities,
        string? NextPageToken);
}
