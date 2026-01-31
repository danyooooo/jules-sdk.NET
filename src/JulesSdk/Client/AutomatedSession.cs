// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using JulesSdk.Http;
using JulesSdk.Models;

namespace JulesSdk.Client;

/// <summary>
/// Represents an automated session handle.
/// </summary>
internal class AutomatedSession : IAutomatedSession
{
    private readonly ApiClient _apiClient;
    private readonly int _pollingIntervalMs;
    
    public string Id { get; }
    
    public AutomatedSession(string sessionId, ApiClient apiClient, int pollingIntervalMs)
    {
        Id = sessionId.Replace("sessions/", "");
        _apiClient = apiClient;
        _pollingIntervalMs = pollingIntervalMs;
    }
    
    public async IAsyncEnumerable<Activity> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
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
                
                yield return activity;
                
                // Check for terminal activities
                if (activity is SessionCompletedActivity or SessionFailedActivity)
                    yield break;
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
    
    public async Task<SessionOutcome> ResultAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var session = await _apiClient.RequestAsync<SessionResource>(
                $"sessions/{Id}",
                cancellationToken: cancellationToken);
                
            if (session.State is SessionState.Completed or SessionState.Failed)
            {
                var pullRequest = session.Outputs?
                    .FirstOrDefault(o => o.PullRequest != null)?
                    .PullRequest;
                    
                return new SessionOutcome
                {
                    SessionId = session.Id,
                    Title = session.Title ?? "",
                    State = session.State,
                    PullRequest = pullRequest,
                    Outputs = session.Outputs ?? []
                };
            }
            
            await Task.Delay(_pollingIntervalMs, cancellationToken);
        }
        
        throw new OperationCanceledException();
    }
    
    private record ListActivitiesResponse(
        IReadOnlyList<Activity>? Activities,
        string? NextPageToken);
}
