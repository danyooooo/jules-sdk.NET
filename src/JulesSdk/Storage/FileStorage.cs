// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using JulesSdk.Models;

namespace JulesSdk.Storage;

/// <summary>
/// File-based implementation of session storage using JSON files.
/// </summary>
public class FileStorage : ISessionStorage
{
    private readonly string _baseDir;
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    
    public FileStorage(string baseDir)
    {
        _baseDir = baseDir;
        Directory.CreateDirectory(_baseDir);
        Directory.CreateDirectory(Path.Combine(_baseDir, "sessions"));
        Directory.CreateDirectory(Path.Combine(_baseDir, "activities"));
    }
    
    public async Task<SessionResource?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var path = GetSessionPath(sessionId);
        if (!File.Exists(path))
            return null;
            
        using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SessionResource>(stream, JsonOptions, cancellationToken);
    }
    
    public async Task UpsertSessionAsync(SessionResource session, CancellationToken cancellationToken = default)
    {
        var path = GetSessionPath(session.Name);
        using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, session, JsonOptions, cancellationToken);
    }
    
    public Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var sessionPath = GetSessionPath(sessionId);
        if (File.Exists(sessionPath))
            File.Delete(sessionPath);
            
        var activityDir = GetActivityDir(sessionId);
        if (Directory.Exists(activityDir))
            Directory.Delete(activityDir, true);
            
        return Task.CompletedTask;
    }
    
    public async IAsyncEnumerable<SessionResource> ListSessionsAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(_baseDir, "sessions");
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            SessionResource? session = null;
            try
            {
                using var stream = File.OpenRead(file);
                session = await JsonSerializer.DeserializeAsync<SessionResource>(stream, JsonOptions, cancellationToken);
            }
            catch { /* Ignore malformed files */ }
            
            if (session != null)
                yield return session;
        }
    }
    
    public async IAsyncEnumerable<Activity> ListActivitiesAsync(
        string sessionId, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var dir = GetActivityDir(sessionId);
        if (!Directory.Exists(dir))
            yield break;
            
        var activities = new List<Activity>();
        
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                using var stream = File.OpenRead(file);
                var activity = await JsonSerializer.DeserializeAsync<Activity>(stream, JsonOptions, cancellationToken);
                if (activity != null)
                    activities.Add(activity);
            }
            catch { /* Ignore malformed */ }
        }
        
        // Sort by creation time
        foreach (var activity in activities.OrderBy(a => a.CreateTime))
        {
            yield return activity;
        }
    }
    
    public async Task UpsertActivityAsync(string sessionId, Activity activity, CancellationToken cancellationToken = default)
    {
        var dir = GetActivityDir(sessionId);
        Directory.CreateDirectory(dir);
        
        var path = Path.Combine(dir, $"{Sanitize(activity.Name)}.json");
        using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, activity, JsonOptions, cancellationToken);
    }
    
    public Task DeleteActivityAsync(string sessionId, string activityId, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(GetActivityDir(sessionId), $"{Sanitize(activityId)}.json");
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }
    
    private string GetSessionPath(string sessionId) => 
        Path.Combine(_baseDir, "sessions", $"{Sanitize(sessionId)}.json");
        
    private string GetActivityDir(string sessionId) => 
        Path.Combine(_baseDir, "activities", Sanitize(sessionId));

    private static string Sanitize(string id) =>
        string.Join("_", id.Split(Path.GetInvalidFileNameChars()));
}
