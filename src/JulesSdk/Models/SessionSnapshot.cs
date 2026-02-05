// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JulesSdk.Options;

namespace JulesSdk.Models;

/// <summary>
/// A point-in-time, immutable view of a session with all activities loaded and derived analytics computed.
/// </summary>
public class SessionSnapshot
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// The unique ID of the session.
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// The current state of the session.
    /// </summary>
    public SessionState State { get; }
    
    /// <summary>
    /// The URL to view the session in the Jules web app.
    /// </summary>
    public string? Url { get; }
    
    /// <summary>
    /// The time the session was created.
    /// </summary>
    public DateTime CreatedAt { get; }
    
    /// <summary>
    /// The time the session was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; }
    
    /// <summary>
    /// The duration of the session.
    /// </summary>
    public TimeSpan Duration => UpdatedAt - CreatedAt;
    
    /// <summary>
    /// The initial prompt for the session.
    /// </summary>
    public string? Prompt { get; }
    
    /// <summary>
    /// The title of the session.
    /// </summary>
    public string? Title { get; }
    
    /// <summary>
    /// The primary Pull Request created by the session, if any.
    /// </summary>
    public PullRequest? PullRequest { get; }
    
    /// <summary>
    /// All activities in the session.
    /// </summary>
    public IReadOnlyList<Activity> Activities { get; }
    
    /// <summary>
    /// Count of activities by type.
    /// </summary>
    public IReadOnlyDictionary<string, int> ActivityCounts { get; }
    
    /// <summary>
    /// Computed timeline of session events.
    /// </summary>
    public IReadOnlyList<TimelineEntry> Timeline { get; }
    
    /// <summary>
    /// Computed insights and analytics.
    /// </summary>
    public SessionInsights Insights { get; }
    
    /// <summary>
    /// Generated files from the session.
    /// </summary>
    public GeneratedFiles GeneratedFiles { get; }
    
    /// <summary>
    /// Creates a new SessionSnapshot from a SessionResource and its activities.
    /// </summary>
    private readonly ChangeSetData? _changeSet;

    /// <summary>
    /// Creates a new SessionSnapshot from a SessionResource and its activities.
    /// </summary>
    public SessionSnapshot(SessionResource session, IReadOnlyList<Activity>? activities = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        
        Id = session.Id;
        State = session.State;
        Url = session.Url;
        Prompt = session.Prompt;
        Title = session.Title;
        Activities = activities ?? [];
        
        // Parse timestamps
        CreatedAt = DateTime.TryParse(session.CreateTime, out var created) 
            ? created 
            : DateTime.MinValue;
        UpdatedAt = DateTime.TryParse(session.UpdateTime, out var updated) 
            ? updated 
            : DateTime.MinValue;
        
        // Prefer Outcome if available (matches TypeScript snapshot.ts logic)
        if (session.Outcome != null)
        {
            PullRequest = session.Outcome.PullRequest;
            _changeSet = session.Outcome.ChangeSet;
        }
        else
        {
            // Fallback: extract PR from outputs
            PullRequest = session.Outputs?
                .FirstOrDefault(o => o.PullRequest != null)?
                .PullRequest;
            
            _changeSet = ComputeChangeSetFallback();
        }
        
        // Compute derived views
        ActivityCounts = ComputeActivityCounts();
        Timeline = ComputeTimeline();
        Insights = ComputeInsights();
        GeneratedFiles = ComputeGeneratedFiles();
    }
    
    /// <summary>
    /// Returns the changeset from the session outcome or activities.
    /// </summary>
    public ChangeSetData? ChangeSet() => _changeSet;
    
    private ChangeSetData? ComputeChangeSetFallback()
    {
        foreach (var activity in Activities)
        {
            if (activity.Artifacts == null) continue;
            
            foreach (var artifact in activity.Artifacts)
            {
                if (artifact.ChangeSet != null)
                    return artifact.ChangeSet;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Serializes the snapshot to JSON with optional field masking.
    /// </summary>
    public string ToJson(SnapshotSerializeOptions? options = null)
    {
        var full = new Dictionary<string, object?>
        {
            ["id"] = Id,
            ["state"] = State.ToString().ToLowerInvariant(),
            ["url"] = Url,
            ["createdAt"] = CreatedAt.ToString("O"),
            ["updatedAt"] = UpdatedAt.ToString("O"),
            ["durationMs"] = (long)Duration.TotalMilliseconds,
            ["prompt"] = Prompt,
            ["title"] = Title,
            ["activities"] = Activities,
            ["activityCounts"] = ActivityCounts,
            ["timeline"] = Timeline,
            ["generatedFiles"] = GeneratedFiles.All(),
            ["insights"] = new
            {
                completionAttempts = Insights.CompletionAttempts,
                planRegenerations = Insights.PlanRegenerations,
                userInterventions = Insights.UserInterventions,
                failedCommandCount = Insights.FailedCommands.Count
            },
            ["pr"] = PullRequest
        };
        
        // Apply field masking
        if (options?.Include != null)
        {
            var filtered = full
                .Where(kvp => options.Include.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return JsonSerializer.Serialize(filtered, JsonOptions);
        }
        
        if (options?.Exclude != null)
        {
            var filtered = full
                .Where(kvp => !options.Exclude.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return JsonSerializer.Serialize(filtered, JsonOptions);
        }
        
        return JsonSerializer.Serialize(full, JsonOptions);
    }
    
    /// <summary>
    /// Generates a markdown summary of the session snapshot.
    /// </summary>
    public string ToMarkdown()
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine($"# Session: {Title ?? Id}");
        sb.AppendLine($"**Status**: `{State}` | **ID**: `{Id}`");
        sb.AppendLine();
        
        // Overview
        sb.AppendLine("## Overview");
        sb.AppendLine($"- **Duration**: {Duration.TotalSeconds:F1}s");
        sb.AppendLine($"- **Total Activities**: {Activities.Count}");
        
        if (PullRequest != null)
        {
            sb.AppendLine($"- **Pull Request**: [{PullRequest.Title}]({PullRequest.Url})");
        }
        
        var files = GeneratedFiles.All();
        if (files.Count > 0)
        {
            sb.AppendLine($"- **Generated Files**: {files.Count}");
            foreach (var file in files.Take(5))
            {
                sb.AppendLine($"  - `{file.Path}` ({file.ChangeType}, +{file.Additions}/-{file.Deletions})");
            }
            if (files.Count > 5)
            {
                sb.AppendLine($"  - _...and {files.Count - 5} more_");
            }
        }
        sb.AppendLine();
        
        // Insights
        sb.AppendLine("## Insights");
        sb.AppendLine($"- **Completion Attempts**: {Insights.CompletionAttempts}");
        sb.AppendLine($"- **Plan Regenerations**: {Insights.PlanRegenerations}");
        sb.AppendLine($"- **User Interventions**: {Insights.UserInterventions}");
        sb.AppendLine($"- **Failed Commands**: {Insights.FailedCommands.Count}");
        sb.AppendLine();
        
        // Timeline
        sb.AppendLine("## Timeline");
        if (Timeline.Count == 0)
        {
            sb.AppendLine("_No activities recorded._");
        }
        else
        {
            foreach (var entry in Timeline.Take(20))
            {
                sb.AppendLine($"- **[{entry.Type}]** {entry.Summary} _({entry.Time})_");
            }
            if (Timeline.Count > 20)
            {
                sb.AppendLine($"- _...and {Timeline.Count - 20} more activities_");
            }
        }
        sb.AppendLine();
        
        // Activity Counts
        if (ActivityCounts.Count > 0)
        {
            sb.AppendLine("## Activity Counts");
            sb.AppendLine("```");
            foreach (var (type, count) in ActivityCounts)
            {
                sb.AppendLine($"{type,-20}: {count}");
            }
            sb.AppendLine("```");
        }
        
        return sb.ToString();
    }
    
    private IReadOnlyDictionary<string, int> ComputeActivityCounts()
    {
        var counts = new Dictionary<string, int>();
        foreach (var activity in Activities)
        {
            var type = activity.Type;
            counts[type] = counts.TryGetValue(type, out var count) ? count + 1 : 1;
        }
        return counts;
    }
    
    private IReadOnlyList<TimelineEntry> ComputeTimeline()
    {
        return Activities
            .Select(a => new TimelineEntry(a.CreateTime ?? "", a.Type, GenerateSummary(a)))
            .ToList();
    }
    
    private static string GenerateSummary(Activity activity)
    {
        // Use helper properties from Activity class
        if (activity.IsAgentMessaged)
        {
            var msg = activity.Message ?? "";
            return $"Agent: {(msg.Length > 100 ? msg[..100] + "..." : msg)}";
        }
        
        if (activity.IsUserMessaged)
        {
            var msg = activity.Message ?? "";
            return $"User: {(msg.Length > 100 ? msg[..100] + "..." : msg)}";
        }
        
        if (activity.IsPlanGenerated)
        {
            var stepCount = activity.PlanGenerated?.Plan?.Steps?.Count ?? 0;
            return $"Plan with {stepCount} steps";
        }
        
        if (activity.IsPlanApproved)
            return "Plan approved";
        
        if (activity.ProgressUpdated != null)
        {
            var title = activity.ProgressUpdated?.Title;
            var desc = activity.ProgressUpdated?.Description;
            return title ?? desc ?? "Progress update";
        }
        
        if (activity.IsSessionCompleted)
            return "Session completed";
        
        if (activity.IsSessionFailed)
        {
            var reason = activity.SessionFailed?.Reason ?? "Unknown";
            return $"Failed: {reason}";
        }
        
        return activity.Type;
    }
    
    private SessionInsights ComputeInsights()
    {
        var failedCommands = Activities
            .Where(a => a.Artifacts?.Any(art => 
                art.BashOutput?.ExitCode is int code && code != 0) ?? false)
            .ToList();
        
        return new SessionInsights
        {
            CompletionAttempts = ActivityCounts.TryGetValue("sessionCompleted", out var c) ? c : 0,
            PlanRegenerations = ActivityCounts.TryGetValue("planGenerated", out var p) ? p : 0,
            UserInterventions = ActivityCounts.TryGetValue("userMessaged", out var u) ? u : 0,
            FailedCommands = failedCommands
        };
    }
    
    private GeneratedFiles ComputeGeneratedFiles()
    {
        var files = new List<GeneratedFile>();
        
        // If we have a definitive changeset (from Outcome or first artifact), use it.
        // This matches the TypeScript behavior of preferring the Outcome's changeset/files.
        if (_changeSet?.GitPatch?.UnidiffPatch != null)
        {
            var parsed = Utilities.UnidiffParser.ParseWithContent(_changeSet.GitPatch.UnidiffPatch);
            files.AddRange(parsed);
        }
        else
        {
            // Fallback: collect from all activities (legacy behavior)
            foreach (var activity in Activities)
            {
                if (activity.Artifacts == null) continue;
                
                foreach (var artifact in activity.Artifacts)
                {
                    if (artifact.ChangeSet?.GitPatch?.UnidiffPatch != null)
                    {
                        var parsed = Utilities.UnidiffParser.ParseWithContent(
                            artifact.ChangeSet.GitPatch.UnidiffPatch);
                        files.AddRange(parsed);
                    }
                }
            }
        }
        
        return new GeneratedFiles(files);
    }
}
