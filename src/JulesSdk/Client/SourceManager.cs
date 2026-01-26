// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using JulesSdk.Http;
using JulesSdk.Models;

namespace JulesSdk.Client;

/// <summary>
/// Manages source connections.
/// </summary>
internal class SourceManager : ISourceManager
{
    private readonly ApiClient _apiClient;
    
    public SourceManager(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    
    public async IAsyncEnumerable<Source> ListAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? pageToken = null;
        
        do
        {
            var query = new Dictionary<string, string> { ["pageSize"] = "100" };
            if (!string.IsNullOrEmpty(pageToken))
                query["pageToken"] = pageToken;
                
            var response = await _apiClient.RequestAsync<ListSourcesResponse>(
                "sources",
                new ApiRequestOptions { Query = query },
                cancellationToken);
                
            foreach (var source in response.Sources ?? [])
            {
                yield return source;
            }
            
            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));
    }
    
    public async Task<Source?> GetAsync(string github, CancellationToken cancellationToken = default)
    {
        // Parse owner/repo format
        var parts = github.Split('/');
        if (parts.Length != 2)
            return null;
            
        var owner = parts[0];
        var repo = parts[1];
        
        await foreach (var source in ListAsync(cancellationToken))
        {
            if (source.GitHubRepo?.Owner == owner && source.GitHubRepo?.Repo == repo)
                return source;
        }
        
        return null;
    }
    
    public async Task<Source> GetByNameAsync(string sourceName, CancellationToken cancellationToken = default)
    {
        // Strip "sources/" prefix if provided
        var cleanName = sourceName.StartsWith("sources/") 
            ? sourceName.Substring("sources/".Length) 
            : sourceName;
            
        return await _apiClient.RequestAsync<Source>(
            $"sources/{cleanName}",
            cancellationToken: cancellationToken);
    }
    
    private record ListSourcesResponse(
        IReadOnlyList<Source>? Sources,
        string? NextPageToken);
}
