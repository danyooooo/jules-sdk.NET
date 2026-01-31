// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using System.Text.Json;
using JulesSdk.Exceptions;
using JulesSdk.Http;
using JulesSdk.Models;
using JulesSdk.Options;

namespace JulesSdk.Client;

using JulesSdk.Storage;

/// <summary>
/// Implementation of the main JulesClient interface.
/// </summary>
internal class JulesClientImpl : IJulesClient, IDisposable
{
    private readonly ApiClient _apiClient;
    private readonly JulesOptions _options;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private bool _disposed;
    
    public ISourceManager Sources { get; }
    public ISessionStorage Storage { get; }
    
    public JulesClientImpl(HttpClient httpClient, JulesOptions options, bool ownsHttpClient = false)
    {
        _httpClient = httpClient;
        _options = options;
        _ownsHttpClient = ownsHttpClient;
        _apiClient = new ApiClient(httpClient, options);
        Sources = new SourceManager(_apiClient);
        
        if (!string.IsNullOrEmpty(options.CacheDir))
        {
            Storage = new FileStorage(options.CacheDir);
        }
        else
        {
            Storage = new MemoryStorage();
        }
    }
    
    public async Task<IAutomatedSession> RunAsync(SessionConfig config, CancellationToken cancellationToken = default)
    {
        var body = await PrepareSessionCreationAsync(config, cancellationToken);
        body["automationMode"] = config.AutoPr == false ? "AUTOMATION_MODE_UNSPECIFIED" : "AUTO_CREATE_PR";
        body["requirePlanApproval"] = config.RequireApproval ?? false;
        
        var session = await _apiClient.RequestAsync<SessionResource>(
            "sessions",
            new ApiRequestOptions { Method = HttpMethod.Post, Body = body },
            cancellationToken);
            
        return new AutomatedSession(session.Id, _apiClient, _options.PollingIntervalMs);
    }
    
    public async Task<ISessionClient> SessionAsync(SessionConfig config, CancellationToken cancellationToken = default)
    {
        var body = await PrepareSessionCreationAsync(config, cancellationToken);
        body["automationMode"] = "AUTOMATION_MODE_UNSPECIFIED";
        body["requirePlanApproval"] = config.RequireApproval ?? true;
        
        var session = await _apiClient.RequestAsync<SessionResource>(
            "sessions",
            new ApiRequestOptions { Method = HttpMethod.Post, Body = body },
            cancellationToken);
            
        return new SessionClientImpl(session.Id, _apiClient, _options.PollingIntervalMs);
    }
    
    public ISessionClient Session(string sessionId)
    {
        return new SessionClientImpl(sessionId.Replace("sessions/", ""), _apiClient, _options.PollingIntervalMs);
    }
    
    public async IAsyncEnumerable<SessionResource> SessionsAsync(
        ListSessionsOptions? options = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= new ListSessionsOptions();
        var pageToken = options.PageToken;
        var count = 0;
        
        while (true)
        {
            var query = new Dictionary<string, string>
            {
                ["pageSize"] = options.PageSize.ToString()
            };
            if (!string.IsNullOrEmpty(pageToken))
                query["pageToken"] = pageToken;
                
            var response = await _apiClient.RequestAsync<ListSessionsResponse>(
                "sessions",
                new ApiRequestOptions { Query = query },
                cancellationToken);
                
            foreach (var session in response.Sessions ?? [])
            {
                yield return session;
                count++;
                
                if (options.Limit.HasValue && count >= options.Limit.Value)
                    yield break;
            }
            
            if (string.IsNullOrEmpty(response.NextPageToken))
                yield break;
                
            pageToken = response.NextPageToken;
        }
    }
    
    public async Task<IReadOnlyList<IAutomatedSession>> AllAsync<T>(
        IEnumerable<T> items, 
        Func<T, SessionConfig> mapper, 
        BatchOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        options ??= new BatchOptions();
        var results = new List<IAutomatedSession>();
        var semaphore = new SemaphoreSlim(options.Concurrency);
        var tasks = new List<Task<IAutomatedSession>>();
        
        foreach (var item in items)
        {
            await semaphore.WaitAsync(cancellationToken);
            
            if (options.DelayMs > 0 && tasks.Count > 0)
                await Task.Delay(options.DelayMs, cancellationToken);
            
            var task = Task.Run(async () =>
            {
                try
                {
                    var config = mapper(item);
                    return await RunAsync(config, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);
            
            tasks.Add(task);
        }
        
        if (options.StopOnError)
        {
            results.AddRange(await Task.WhenAll(tasks));
        }
        else
        {
            foreach (var task in tasks)
            {
                try
                {
                    results.Add(await task);
                }
                catch
                {
                    // Continue on error
                }
            }
        }
        
        return results;
    }
    
    public async Task<SyncStats> SyncAsync(SyncOptions? options = null, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        options ??= new SyncOptions();
        
        var sessionsIngested = 0;
        var activitiesIngested = 0;
        
        // For now, a simple implementation that fetches sessions
        await foreach (var session in SessionsAsync(new ListSessionsOptions { Limit = options.Limit }, cancellationToken))
        {
            await Storage.UpsertSessionAsync(session, cancellationToken);
            sessionsIngested++;
            
            if (options.Depth == SyncDepth.Activities)
            {
                // Fetch activities for each session
                var sessionClient = Session(session.Id);
                await foreach (var activity in sessionClient.HistoryAsync(null, cancellationToken))
                {
                    await Storage.UpsertActivityAsync(session.Id, activity, cancellationToken);
                    activitiesIngested++;
                }
            }
        }
        
        return new SyncStats(
            sessionsIngested,
            activitiesIngested,
            true,
            (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
    }
    
    public IJulesClient With(JulesOptions options)
    {
        var mergedOptions = new JulesOptions
        {
            ApiKey = options.ApiKey ?? _options.ApiKey,
            PollingIntervalMs = options.PollingIntervalMs > 0 ? options.PollingIntervalMs : _options.PollingIntervalMs,
            RequestTimeoutMs = options.RequestTimeoutMs > 0 ? options.RequestTimeoutMs : _options.RequestTimeoutMs,
            RateLimitRetry = options.RateLimitRetry ?? _options.RateLimitRetry,
            CacheDir = options.CacheDir ?? _options.CacheDir
        };
        return new JulesClientImpl(_httpClient, mergedOptions);
    }
    
    private async Task<Dictionary<string, object>> PrepareSessionCreationAsync(
        SessionConfig config, 
        CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object>
        {
            ["prompt"] = config.Prompt
        };
        
        if (!string.IsNullOrEmpty(config.Title))
            body["title"] = config.Title;
            
        if (config.Source != null)
        {
            var source = await Sources.GetAsync(config.Source.Github, cancellationToken);
            if (source == null)
                throw new SourceNotFoundException(config.Source.Github);
                
            body["sourceContext"] = new
            {
                source = source.Name,
                githubRepoContext = new
                {
                    startingBranch = config.Source.BaseBranch
                }
            };
        }
        
        return body;
    }
    
    private record ListSessionsResponse(
        IReadOnlyList<SessionResource>? Sessions,
        string? NextPageToken);
    
    /// <summary>
    /// Disposes resources used by the client.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Disposes resources used by the client.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing && _ownsHttpClient)
        {
            _httpClient.Dispose();
        }
        
        _disposed = true;
    }
}
